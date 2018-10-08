window.Status = (function() {

    var loadersList = {},
        registeredRefreshes = {},
        refreshInteralMultiplier = 1;

    function registerRefresh(name, callback, interval, paused) {
        var refreshData = {
            name: name,
            func: callback,
            interval: interval,
            paused: paused // false on init almost always
        };
        registeredRefreshes[name] = refreshData;
        refreshData.timer = setTimeout(function() { execRefresh(refreshData); }, refreshData.interval * refreshInteralMultiplier);
    }

    function setRefreshInterval(val) {
        if (val === 0) return; // don't do that
        console.log('Setting refresh speed to ' + (100/val).toFixed(0) + '% of normal.');
        refreshInteralMultiplier = val;
    }

    function getRefresh(name) {
        return registeredRefreshes[name];
    }

    function runRefresh(name) {
        console.log('Forcing a full refresh.');
        pauseRefresh(name, true);
        resumeRefresh(name, true);
    }

    function scheduleRefresh(ms) {
        return setTimeout(runRefresh, ms);
    }

    function execRefresh(refreshData) {
        if (refreshData.paused) {
            return;
        }
        var def = refreshData.func();
        if (typeof (def.done) === 'function') {
            def.done(function() {
                refreshData.timer = setTimeout(function() { execRefresh(refreshData); }, refreshData.interval * refreshInteralMultiplier);
            });
        }

        refreshData.running = def;
        refreshData.timer = 0;
    }

    function pauseRefresh(name, silent) {
        function pauseSingleRefresh(r) {
            r.paused = true;
            if (r.timer) {
                clearTimeout(r.timer);
                r.timer = 0;
            }
            if (r.running) {
                if (typeof (r.running.reject) === 'function') {
                    r.running.reject();
                }
                if (typeof (r.running.abort) === 'function') {
                    r.running.abort();
                }
            }
        }

        if (name && registeredRefreshes[name]) {
            if (!silent) {
                console.log('Refresh paused for: ' + name);
            }
            pauseSingleRefresh(registeredRefreshes[name]);
            return;
        }

        if (!silent) {
            console.log('Refresh paused');
        }
        for (var key in registeredRefreshes) {
            if (registeredRefreshes.hasOwnProperty(key)) {
                pauseSingleRefresh(registeredRefreshes[key]);
            }
        }
    }

    function resumeRefresh(name, silent) {
        function resumeSingleRefresh(r) {
            if (r.timer) {
                clearTimeout(r.timer);
            }
            r.paused = false;
            execRefresh(r);
        }

        if (name && registeredRefreshes[name]) {
            if (!silent) {
                console.log('Refresh resumed for: ' + name);
            }
            resumeSingleRefresh(registeredRefreshes[name]);
            return;
        }

        if (!silent) {
            console.log('Refresh resumed');
        }
        for (var key in registeredRefreshes) {
            if (registeredRefreshes.hasOwnProperty(key)) {
                resumeSingleRefresh(registeredRefreshes[key]);
            }
        }
    }

    var currentDialog = null,
        prevHash = null;

    function closePopup() {
        if (currentDialog) {
            var dialog = currentDialog;
            currentDialog = null;
            dialog.modal('hide');
        }
    }

    function popup(url, data, options) {
        closePopup();

        var hash = window.location.hash,
            dialog = currentDialog = bootbox.dialog({
            message: '<div class="modal-loader js-summary-popup"></div>',
            title: 'Loading...',
            size: 'large',
            backdrop: true,
            buttons: options && options.buttons,
            onEscape: function () { }
        });

        dialog.on('hide.bs.modal', function () {
            var l = window.location;
            if (hash === l.hash) { // Only clear when we aren't shifting hashes
                if ('pushState' in history) {
                    history.pushState('', document.title, l.pathname + l.search);
                    hashChangeHandler();
                } else {
                    l.hash = '';
                }
            }
            if (options && options.onClose) {
                options.onClose.call(this);
            }
        });
        dialog.on('hidden.bs.modal', function() {
            if ($('.bootbox').length) {
                $('body').addClass('modal-open');
            }
        });
        if (options && options.modalClass) {
            dialog.find('.modal-lg').removeClass('modal-lg').addClass(options.modalClass);
        }

        // TODO: refresh intervals via header
        $('.js-summary-popup')
            .appendWaveLoader()
            .load(Status.options.rootPath + url, data, function (responseText, textStatus, req) {
                if (textStatus === 'error') {
                    $(this).closest('.modal-content').find('.modal-header .modal-title').addClass('text-warning').text('Error');
                    $(this).html('<div class="alert alert-warning"><h5>Error loading</h5><p class="error-stack">Status: ' + req.statusText + '\nCode: ' + req.status + '\nUrl: ' + url + '</p></div><p>Direct link: <a href="' + url + '">' + url + '</a></p>');
                    return;
                }
                var titleElem = $(this).findWithSelf('h4.modal-title');
                if (titleElem) {
                    $(this).closest('.modal-content').find('.modal-header .modal-title').replaceWith(titleElem);
                }
                if (options && options.onLoad) {
                    options.onLoad.call(this);
                }
            });
        return dialog;
    }

    function hashChangeHandler(firstLoad) {
        var hash = window.location.hash;
        if (!hash || hash.length > 1) {
            for (var h in loadersList) {
                if (loadersList.hasOwnProperty(h) && hash.indexOf(h) === 0) {
                    var val = hash.replace(h, '');
                    loadersList[h](val, firstLoad, prevHash);
                }
            }
        }
        if (!hash) {
            closePopup();
        }
        prevHash = hash;
    }

    function registerLoaders(loaders) {
        $.extend(loadersList, loaders);
    }

    $(window).on('hashchange', function () { hashChangeHandler(); });
    $(function() {
        // individual sections add via Status.loaders.register(), delay running until after they're added on-load
        setTimeout(function () { hashChangeHandler(true); }, 1);
    });

    function prepTableSorter() {
        $.tablesorter.addParser({
            id: 'relativeDate',
            is: function() { return false; },
            format: function(s, table, cell) {
                var date = $(cell).find('.js-relative-time').attr('title'); // e.g. 2011-03-31 01:57:59Z
                if (!date)
                    return 0;

                var exp = /(\d{4})-(\d{1,2})-(\d{1,2})\W*(\d{1,2}):(\d{1,2}):(\d{1,2})Z/i.exec(date);
                return new Date(exp[1], exp[2], exp[3], exp[4], exp[5], exp[6], 0).getTime();
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'commas',
            is: function() { return false; },
            format: function(s) {
                return s.replace('$', '').replace(/,/g, '');
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'dataVal',
            is: function() { return false; },
            format: function(s, table, cell) {
                return $(cell).data('val') || 0;
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'cellText',
            is: function() { return false; },
            format: function(s, table, cell) {
                return $(cell).text();
            },
            type: 'text'
        });
    }

    function init(options) {
        Status.options = options;

        if (options.HeaderRefresh) {
            Status.refresh.register('TopBar', function() {
                return $.ajax(options.rootPath + 'top-refresh', {
                    data: { tab: Status.options.Tab }
                }).done(function (html) {
                    var resp = $(html);
                    var tabs = $('.js-top-tabs', resp);
                    if (tabs.length) {
                        $('.js-top-tabs').replaceWith(tabs);
                    }
                    var issuesList = $('.js-issues-button', resp);
                    var curList = $('.js-issues-button');
                    if (issuesList.length) {
                        // Re-think what comes down here, plan for websockets
                        var issueCount = issuesList.data('count');
                        // TODO: don't if hovering
                        if (issueCount > 0) {
                            if (!curList.children().length) {
                                curList.replaceWith(issuesList.find('.issues-button').fadeIn().end());
                            } else {
                                $('.issues-button').html($('.issues-button', issuesList).html());
                                $('.issues-dropdown').html($('.issues-dropdown', issuesList).html());
                            }
                        } else {
                            curList.fadeOut(function() {
                                $(this).empty();
                            });
                        }
                    }
                }).fail(Status.UI.ajaxError);
            }, Status.options.HeaderRefresh * 1000);
        }
        
        registerLoaders({
            '#/issues': function (val) {
                Status.popup('issues');
            }
        });

        var resizeTimer, dropdownPause;
        $(window).resize(function() {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function() {
                $(this).trigger('resized');
            }, 100);
        });
        $(document).on('click', '.js-reload-link', function(e) {
            var data = {
                type: $(this).data('type'),
                key: $(this).data('uk'),
                guid: $(this).data('guid')
            };
            if (!data.type && ($(this).attr('href') || '#') !== '#') return;
            e.preventDefault();
            if (data.type && data.key) {
                // Node to refresh, do it
                if ($(this).hasClass('active')) return;
                var link = $(this).addClass('active');
                link.find('.fa').addClass('fa-spin');
                link.find('.js-text').text('Polling...');
                Status.refresh.pause();
                $.post(Status.options.rootPath + 'poll', data)
                    .fail(function() {
                        toastr.error('There was an error polling this node.', 'Polling',
                        {
                            positionClass: 'toast-top-right-spaced',
                            timeOut: 5 * 1000
                        });
                    }).done(function () {
                        if (link.closest('.js-refresh').length) {
                            // TODO: Refresh only this section, not all others
                            // Possibly pause refresh on refreshing sections via an upfront
                            // .closest('.js-refresh').addClass()
                            Status.refresh.resume();
                        } else {
                            window.location.reload(true);
                        }
                        //link.removeClass('reloading')
                        //    .find('.js-text')
                        //    .text('Poll Now');
                    });
            } else {
                window.location.reload(true);
            }
        }).on('click', '.js-poll-now', function () {
            var type = $(this).data('type'),
                key = $(this).data('key');

            if ($(this).hasClass('disabled')) { return false; }

            if (type && key) {
                $(this).text('').prependWaveLoader().addClass('disabled');
                $.ajax(Status.options.rootPath + 'poll', {
                    data: {
                        type: type,
                        key: key,
                        guid: $(this).data('id')
                    }
                }).done(function () {
                    var name = $(this).closest('.js-refresh[data-name]').data('name');
                    if (name) {
                        Status.refresh.get('Dashboard').func(name);
                    } else {
                        window.location.reload(true);
                    }
                });
            }
            return false;
        }).on('click', '.js-dropdown-actions', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var jThis = $(this);
            if (jThis.hasClass('open')) {
                jThis.removeClass('open');
                resumeRefresh();
            } else {
                $('.js-dropdown-actions.open').removeClass('open');
                var ddSource = $('.js-haproxy-server-dropdown ul').clone();
                jThis.append(ddSource);
                var actions = jThis.data('actions');
                if (actions) {
                    jThis.find('a[data-action]').each(function(_, i) {
                        $(i).toggleClass('disabled', actions.indexOf($(i).data('action')) === -1);
                    });
                }
                jThis.addClass('open');
                pauseRefresh();
            }
        }).on('click', '.js-dropdown-actions a', function (e) {
            e.preventDefault();
        }).on({
            'click': function() {
                $('.action-popup').remove();
                $('.js-dropdown-actions.open').removeClass('open');
            },
            'show': function() {
                setRefreshInterval(1);
                runRefresh();
            },
            'hide': function() {
                setRefreshInterval(10);
            }
        });
        prepTableSorter();
        prettyPrint();
        hljs.initHighlighting();
    }

    return {
        init: init,
        popup: popup,
        loaders: {
            list: loadersList,
            register: registerLoaders
        },
        graphCount: 0,
        refresh: {
            register: registerRefresh,
            pause: pauseRefresh,
            resume: resumeRefresh,
            get: getRefresh,
            run: runRefresh,
            registered: registeredRefreshes,
            schedule: scheduleRefresh
        }
    };
})();

Status.UI = (function () {

    function ajaxError(xhr, status, error, thing) {
        console.log(xhr, status, error, thing);
    }

    return {
        ajaxError: ajaxError
    };
})();

Status.Dashboard = (function () {
    
    function applyFilter(filter) {
        Status.Dashboard.options.filter = filter = (filter || '');
        window.history.replaceState({ 'q': filter }, document.title, Status.options.rootPath + 'dashboard' + (filter? '?q=' + encodeURIComponent(filter) : ''));
        $('.js-filter').addClass('loading');
        Status.refresh.run('Dashboard');
        return;        
    }

    function init(options) {
        Status.Dashboard.options = options;
        
        Status.loaders.register({
            '#/dashboard/graph/': function (val) {
                Status.popup('dashboard/graph/' + val);
            }
        });

        if (options.refresh) {
            Status.refresh.register('Dashboard', function (filter) {
                return $.ajax(Status.Dashboard.options.refreshUrl || window.location.href, {
                    data: $.extend({}, Status.Dashboard.options.refreshData),
                    cache: false
                }).done(function (html) {
                    var resp = $(html);
                    var refreshGroups = resp.find('.js-refresh[data-name]').add(resp.filter('.js-refresh[data-name]'));
                    $('.js-refresh[data-name]').each(function () {
                        var name = $(this).data('name'),
                            match = refreshGroups.filter('[data-name="' + name + '"]');
                        if (filter && name !== filter) {
                            return;
                        }
                        if (!match.length) {
                            console.log('Unable to find: ' + name + '.');
                        } else {
                            $(this).replaceWith(match);
                        }
                    });
                    if (Status.Dashboard.options.afterRefresh)
                        Status.Dashboard.options.afterRefresh();
                    $('.js-filter').removeClass('loading');
                }).fail(function () {
                    console.log('Failed to refresh', this, arguments);
                });
            }, Status.Dashboard.options.refresh * 1000);
        }

        var filterTimer;
        $(document).on('keyup', '.js-filter', function () {
            clearTimeout(filterTimer);
            var filter = this.value;
            filterTimer = setTimeout(function () {
                applyFilter(filter);
            }, 300);
        });
    }

    return {
        init: init
    };

})();

Status.Dashboard.Server = (function () {

    function init(options) {
        Status.Dashboard.Server.options = options;

        Status.loaders.register({
            '#/dashboard/summary/': function (val) {
                Status.popup('dashboard/node/summary/' + val, { node: Status.Dashboard.Server.options.node });
            }
        });

        $('.realtime-cpu').on('click', function () {
            var start = +$(this).parent().siblings('.total-cpu-percent').text().replace(' %','');
            liveDashboard(start);
            return false;
        });
        $('table.js-interfaces').tablesorter({
            headers: {
                1: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                2: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                3: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                4: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                5: { sorter: 'dataVal', sortInitialOrder: 'desc' }
            }
        });

        $(document).on('click', '.js-service-action', function () {
            var link = $(this);
            var $link = link.text('').prependWaveLoader();
            var node = link.closest('[data-node]').data('node');
            $.ajax(Status.options.rootPath + 'dashboard/node/service/action', {
                type: 'POST',
                data: {
                    node: node,
                    name: link.closest('[data-name]').data('name'),
                    serviceAction: link.data('action')
                },
                success: function (data, status, xhr) {
                    if (data.Success === true) {
                        if (node) {
                            Status.refresh.run('Dashboard');
                            window.location.reload(true);
                        }
                    } else {
                        $link.text(link.data('action')).errorPopupFromJSON(xhr, data.Message);
                    }
                },
                error: function (xhr) {
                    $link.text(link.data('action')).errorPopupFromJSON(xhr, data.Message);
                }
            });
            return false;
        });
    }
    
    function liveDashboard(startValue) {

        if ($('#cpu-graph').length) return;

        var container = '<div id="cpu-graph"><div class="cpu-total"><div id="cpu-total-graph" class="chart" data-title="Total CPU Utilization"></div></div></div>',
            wrap = $(container).appendTo('.js-content'),
            liveGraph = $('#cpu-total-graph').lived3graph({
                type: 'cpu',
                width: 'auto',
                height: 250,
                startValue: startValue,
                params: { node: Status.Dashboard.Server.options.node },
                series: [{ name: 'total', label: 'CPU' }],
                max: 100,
                leftMargin: 30,
                areaTooltipFormat: function(value) { return '<span class="label">CPU: </span><b>' + (value || 0).toFixed(0) + '%</b>'; }
            });

        wrap.modal({
            overlayClose: true,
            onClose: function(dialog) {
                dialog.data.fadeOut('fast', function() {
                    dialog.container.hide('fast', function() {
                        $.modal.close();
                        liveGraph.stop();
                        $('#cpu-graph').remove();
                    });
                });
            }
        });
    }
    
    return {
        init: init
    };
})();

Status.Elastic = (function () {

    function init(options) {
        Status.Elastic.options = options;

        Status.loaders.register({
            '#/elastic/node/': function (val) {
                Status.popup('elastic/node/modal/' + val, {
                    cluster: Status.Elastic.options.cluster,
                    node: Status.Elastic.options.node
                });
            },
            '#/elastic/cluster/': function (val) {
                Status.popup('elastic/cluster/modal/' + val, {
                    cluster: Status.Elastic.options.cluster,
                    node: Status.Elastic.options.node
                });
            },
            '#/elastic/index/': function (val) {
                var parts = val.split('/');
                var reqOptions = $.extend({}, options, { index: parts[0] });
                Status.popup('elastic/index/modal/' + parts[1], reqOptions);
            }
        });
    }

    return {
        init: init
    };
})();

Status.NodeSearch = (function () {

    function init(options) {
        Status.NodeSearch.options = options;

        var ac = $('.js-filter').on('click', function() {
            var val = $(this).val();
            $(this).select().autocomplete('search', '').removeClass('icon').val('');
            setTimeout(function() {
                var selected = $('.status-icon[data-host="' + val + '"]');
                if (selected.length) {
                    var top = selected.closest('li').addClass('ac_over').position().top;
                    $('.ac_results ul').scrollTop(top);
                }
            }, 0);
        });
        var ai = ac.autocomplete({
            minLength: 0,
            delay: 25,
            source: options.searchUrl
                ? function (request, response) {
                    $.ajax(options.searchUrl, {
                        dataType: "jsonp",
                        data: { q: request.term },
                        success: function (data) {
                            response(data);
                        }
                    });
                }
                : options.nodes,
            appendTo: ac.parent(),
            messages: {
                noResults: '',
                results: function () { }
            },
            select: function (event, ui) {
                if (options.nodes) {
                    $(this).val(ui.item.value).closest('form').submit();
                }
            }
        }).autocomplete('instance');
        ai._renderMenu = function (ul, items) {
            var that = this;
            $.each(items, function (index, item) {
                that._renderItemData(ul, item);
            });
            $(ul).addClass('dropdown-menu navbar-list');
        };
        ai._renderItem = function(ul, item) {
            var html = '<span class="' + item.icon + '">●</span> '
                + '<span class="text-muted">' + item.category + ':</span> '
                + item.label;
            return $('<li>').data('data-value', item.value).append('<a>' + html + '</a>').appendTo(ul);
        };


        $('.server-search').on('click', '.js-show-all-down', function () {
            $(this).siblings('.hidden').removeClass('hidden').end().remove();
        });
        $('.node-category-list').on('click', '.filters-current', function (e) {
            if (e.target.tagName !== 'INPUT') $('.filters, .filters-toggle').toggle();
        }).on('keyup', '.filter-form', function (e) {
            if (e.keyCode === 13) {
                $(this).submit();
                $('.filters').hide();
                $('.filters-current').addClass('loading');
            }
        });
    }

    return {
        init: init
    };

})();

Status.SQL = (function () {
    
    function loadCluster(val) {
        var pieces = val.split('/');
        Status.Dashboard.options.refreshData = {
            cluster: pieces.length > 0 ? pieces[0] : null,
            ag: pieces.length > 1 ? pieces[1] : null,
            node: pieces.length > 2 ? pieces[2] : null
        };
        Status.popup('sql/servers', $.extend({}, Status.Dashboard.options.refreshData, { detailOnly: true }));
    }

    function loadPlan(val) {
        var parts = val.split('/'),
            handle = parts[0],
            offset = parts.length > 1 ? parts[1] : null;
        $('.plan-row[data-plan-handle="' + handle + '"]').addClass('info');
        Status.popup('sql/top/detail', {
            node: Status.SQL.options.node,
            handle: handle,
            offset: offset
        }, {
            onLoad: function() {
                $(this).closest('.modal-lg').removeClass('modal-lg').addClass('modal-huge');
                prettyPrint();
                if ($('.qp-root').length) {
                    $('.qp-root').drawQueryPlanLines();
                    var currentTt;
                    $(this).find('.qp-node').hover(function () {
                        var pos = $(this).offset();
                        var tt = $(this).find('.qp-tt');
                        currentTt = tt.clone();
                        currentTt.addClass('sql-query-tooltip')
                            .appendTo(document.body)
                            .css({ top: pos.top + $(this).outerHeight(), left: pos.left })
                            .show();
                    }, function () {
                        if (currentTt) currentTt.hide();
                    });
                }
                $(this).find('.js-remove-plan').on('click', function() {
                    if ($(this).hasClass('js-confirm')) {
                        $(this).text('confirm?').removeClass('js-confirm');
                        return false;
                    }

                    var $link = $(this).addClass('loading');
                    $.ajax($link.attr('href'), {
                        type: 'POST',
                        success: function(data, status, xhr) {
                            if (data === true) {
                                window.location.hash = '';
                                window.location.reload(true);
                            } else {
                                $link.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred removing this plan from cache.');
                            }
                        },
                        error: function(xhr) {
                            $link.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred removing this plan from cache.');
                        }
                    });
                    return false;
                });
                $(this).find('.show-toggle').on('click', function(e) {
                    var grad = $(this).closest('.hide-gradient'),
                        excerpt = grad.prev('.sql-query-excerpt');
                    excerpt.animate({ 'max-height': excerpt[0].scrollHeight }, 400, function() { $(window).resize(); });
                    grad.fadeOut(400);
                    e.preventDefault();
                });
            },
            onClose: function() {
                $('.plan-row.selected').removeClass('info');
            }
        });
    }
    
    function init(options) {
        Status.SQL.options = options;

        var filterOptions = {
            modalClass: 'modal-md',
            buttons: {
                "Apply Filters": function (e) {
                    $(this).find('form').submit();
                    return false;
                }
            }
        }
        
        Status.loaders.register({
            '#/cluster/': loadCluster,
            '#/plan/': loadPlan,
            '#/sql/summary/': function (val) {
                Status.popup('sql/instance/summary/' + val, { node: Status.SQL.options.node }, {
                    modalClass: val === 'errors' ? 'modal-huge' : 'modal-lg'
                });
            },
            '#/sql/top/filters': function () {
                Status.popup('sql/top/filters' + window.location.search, null, filterOptions);
            },
            '#/sql/active/filters': function () {
                Status.popup('sql/active/filters' + window.location.search, null, filterOptions);
            },
            '#/db/': function (val, firstLoad, prev) {
                var obj = val.indexOf('tables/') > 0 || val.indexOf('views/') || val.indexOf('storedprocedures/') || val.indexOf('unusedindexes/') > 0
                          ? val.split('/').pop() : null;
                function showColumns() {
                    $('.js-next-collapsible').removeClass('info').next().hide();
                    var cell = $('[data-obj="' + obj + '"]').addClass('info').next().show(200).find('td');
                    if (cell.length === 1) {
                        cell.css('max-width', cell.closest('.js-database-modal-right').width());
                    }
                }
                if (!firstLoad) {
                    // TODO: Generalize this to not need the replace? Possibly a root load in the modal
                    if ((/\/tables/.test(val) && /\/tables/.test(prev)) || (/\/views/.test(val) && /\/views/.test(prev)) || (/\/storedprocedures/.test(val) && /\/storedprocedures/.test(prev)) || (/\/unusedindexes/.test(val) && /\/unusedindexes/.test(prev))) {
                        showColumns();
                        return;
                    }
                }
                Status.popup('sql/db/' + val, { node: Status.SQL.options.node }, {
                    modalClass: 'modal-huge',
                    onLoad: function () {
                        showColumns();
                    }
                });
            }
        });        
        
        $('.js-content').on('click', '.plan-row[data-plan-handle]', function () {
            var $this = $(this),
                handle = $this.data('plan-handle'),
                offset = $this.data('offset');
            if (!handle) return;
            window.location.hash = '#/plan/' + handle + (offset ? '/' + offset : '');
            $('.plan-row.selected').removeClass('selected');
            $this.addClass('selected').find('.query-col').addClass('loading');
        }).on('click', '.filters-current', function () {
            $('.filters').fadeToggle();
        }).on('keyup', '.filter-form', function (e) {
            if (e.keyCode === 13) {
                $(this).submit();
                $('.filters').hide();
                $('.filters-current').addClass('loading');
            }
        }).on('click', '.filters, .filters-current', function (e) {
            e.stopPropagation();
        });
        function agentAction(link, route, errMessage) {
            var origText = link.text();
            var $link = link.text('').prependWaveLoader();
            var perNode = link.closest('[data-node]').data('node');
            $.ajax(Status.options.rootPath + 'sql/' + route, {
                type: 'POST',
                data: {
                    node: perNode || Status.SQL.options.node,
                    guid: link.closest('[data-guid]').data('guid'),
                    enable: link.data('enable')
                },
                success: function (data, status, xhr) {
                    if (data === true) {
                        if (perNode) {
                            Status.refresh.run();
                        } else {
                            Status.popup('sql/instance/summary/jobs', { node: Status.SQL.options.node });
                        }
                    } else {
                        $link.text(origText).errorPopupFromJSON(xhr, errMessage);
                    }
                },
                error: function (xhr) {
                    $link.text(origText).errorPopupFromJSON(xhr, errMessage);
                }
            });
            return false;
        }
        $(document).on('click', function () {
            $('.filters').toggle(false);
        }).on('click', '.js-sql-job-action', function () {
            var link = $(this);
            switch (link.data('action')) {
                case 'toggle':
                    return agentAction($(this), 'toggle-agent-job', 'An error occurred toggling this job.');
                case 'start':
                    return agentAction($(this), 'start-agent-job', 'An error occurred starting this job.');
                case 'stop':
                    return agentAction($(this), 'stop-agent-job', 'An error occurred stopping this job.');
            }
            return false;
        }).on('click', '.ag-node', function() {
            window.location.hash = $('.ag-node-name a', this)[0].hash;
        }).on('click', '.js-next-collapsible', function () {
            window.location.hash = window.location.hash.replace(/\/tables\/.*/, '/tables').replace(/\/views\/.*/, '/views').replace(/\/storedprocedures\/.*/, '/storedprocedures');
        });
    }

    return {
        init: init
    };

})();

Status.Redis = (function () {
    
    function init(options) {
        Status.Redis.options = options;

        Status.loaders.register({
            '#/redis/summary/': function(val) {
                Status.popup('redis/instance/summary/' + val, { node: Status.Redis.options.node + ':' + Status.Redis.options.port });
            },
            '#/redis/actions/': function (val) {
                Status.popup('redis/instance/actions/' + val);
            }
        });

        function runAction(link, options, event) {
            var action = $(link).data('action'),
                modal = $(link).closest('.js-redis-actions'),
                node = modal.data('node'),
                title = modal.data('name'),
                confirmMessage = options && options.confirmMessage || $(link).data('confirm');

            function innerRun() {
                $.post('redis/instance/actions/' + node + '/' + action, options && options.data)
                 .done(options && options.onComplete || function () {
                     Status.refresh.run();
                     bootbox.hideAll();
                 });
            }
            if (confirmMessage && !(event && event.ctrlKey)) {
                bootbox.confirm({
                    title: title,
                    message: confirmMessage,
                    callback: function (result) {
                        if (result) {
                            innerRun();
                        }
                    }
                });
            } else {
                innerRun();
            }
        }
        $(document).on('click', '.js-instance-action', function (e) {
            e.preventDefault();
            runAction(this, null, e);
        }).on('click', '.js-redis-new-master', function (e) {
            var modal = $(this).closest('.js-redis-actions'),
                node = modal.data('node'),
                newMaster = $(this).data('new-master');
            e.preventDefault();
            runAction(this, {
                confirmMessage: 'Are you sure you want make ' + node + ' a slave of ' + newMaster + '?',
                data: {
                    newMaster: newMaster
                }
            }, e);
        }).on('click', '.js-redis-key-purge', function (e) {
            var modal = $(this).closest('.js-redis-actions'),
                key = $('[name=key]', modal).val();
            e.preventDefault();
            runAction(this, {
                data: {
                    db: $('[name=database]', modal).val(),
                    key: key
                },
                onComplete: function (result) {
                    bootbox.alert(result
                        ? 'Keys removed: ' + result.removed
                        : 'Error removing keys');
                }
            });
        }).on('click', '.js-redis-host-list', function (e) {
            $(this).siblings().find('.fa-chevron-down').toggleClass('fa-chevron-down fa-chevron-right text-primary').end().next('.hidden').removeClass('selected').slideUp(150);
            $(this).find('.fa-chevron-right').toggleClass('fa-chevron-down fa-chevron-right text-primary').end().next('.hidden').addClass('selected').slideDown(150);
        }).on('click', '.js-server-actions-selection', function () {
            var url = $(this).data('preview-url');
            var operations = $('.js-redis-host-list + div.selected :input:enabled').serialize();
            $('.js-server-action-preview').html('Loading Preview...');
            $('.js-server-action-execute').prop('disabled', true).text('Loading Preview...');
            $.post(url, operations, function (result) {
                $('.js-server-action-preview').html(result);
                $('.js-server-action-execute').prop('disabled', false).text('Execute');
            });
        }).on('click', '.js-server-action-execute', function (e) {
            var url = $(this).data('perform-url');
            var operations = $('.js-redis-server-actions-preview :input').serialize();
            $.post(url, operations, function (response) {
                if (response.success) {
                    bootbox.hideAll();
                    bootbox.alert(response.result);
                }
            });
        }).on({
            mouseenter: function () {
                Status.refresh.pause();
            },
            mouseleave: function () {
                Status.refresh.resume();
            }
        }, '.js-refresh .dropdown');
    }

    return {
        init: init
    };

})();

Status.Exceptions = (function () {

    function refreshCounts(data) {
        if (!(data.Groups && data.Groups.length)) return;
        var log = Status.Exceptions.options.log,
            logCount = 0,
            group = Status.Exceptions.options.group,
            groupCount = 0;
        // For any not found...
        $('.js-exception-total').text('0');
        data.Stores.forEach(function (s) {
            $('.js-exception-total.js-exception-store[data-name="' + s.Name + '"]').text(s.Total.toLocaleString());
        });
        data.Groups.forEach(function(g) {
            if (g.Name === group) {
                groupCount = g.Total;
            }
            $('.js-exception-total[data-name="' + g.Name + '"]').text(g.Total.toLocaleString());
            g.Applications.forEach(function(app) {
                if (app.Name === log) {
                    logCount = app.Total;
                }
                $('.js-exception-total[data-name="' + g.Name + '-' + app.Name + '"]').text(app.Total.toLocaleString());
            });
        });
        if (Status.Exceptions.options.search) return;
        function setTitle(name, count) {
            $('.js-exception-title').html(count.toLocaleString() + ' ' + name + ' Exception' + (count !== 1 ? 's' : ''));
        }

        if (log) {
            setTitle(log, logCount);
        } else if (group) {
            setTitle(group, groupCount);
        } else {
            setTitle('', data.Total);
        }
        $('.js-top-tabs .badge[data-name="Exceptions"]').text(data.Total.toLocaleString());
    }

    function init(options) {
        Status.Exceptions.options = options;

        var baseOptions = {
            store: options.store,
            group: options.group,
            log: options.log,
            sort: options.sort
        };

        // TODO: Set refresh params
        function getLoadCount() {
            // If scrolled back to the top, load 500
            if ($(window).scrollTop() === 0) {
                return 500;
            }
            return Math.max($('.js-exceptions tbody tr').length, 500);
        }

        if (options.refresh) {
            Status.Dashboard.init({ refresh: Status.Exceptions.options.refresh });
        }

        var loadingMore = false,
            allDone = false,
            lastSelected;

        function loadMore() {
            if (loadingMore || allDone) return;

            if ($(window).scrollTop() + $(window).height() > $(document).height() - 100) {
                loadingMore = true;
                $('.js-bottom-loader').show();
                var lastGuid = $('.js-exceptions tr.js-error').last().data('id');
                $.ajax(Status.options.rootPath + 'exceptions/load-more', {
                    data: $.extend({}, baseOptions, {
                        count: options.loadMore,
                        prevLast: lastGuid,
                        q: options.search,
                        showDeleted: options.showDeleted
                    }),
                    cache: false
                }).done(function (html) {
                    var newRows = $(html).filter('tr');
                    $('.js-exceptions tbody').append(newRows);
                    $('.js-bottom-loader').hide();
                    if (newRows.length < options.loadMore) {
                        allDone = true;
                        $('.js-bottom-no-more').fadeIn();
                    }
                    loadingMore = false;
                });
            }
        }

        if (options.loadMore) {
            allDone = $('.js-exceptions tbody tr.js-error').length < 250;
            $(document).on('scroll exception-deleted', loadMore);
        }

        function deleteError(elem) {
            var jThis = $(elem),
                url = jThis.attr('href'),
                jRow = jThis.closest('tr'),
                jCell = jThis.closest('td');

            $(elem).addClass('icon-rotate-flip');

            if (!options.showingDeleted) {
                jRow.hide();
            }

            $.ajax({
                type: 'POST',
                data: $.extend({}, baseOptions, {
                    log: jRow.data('log') || options.log,
                    id: jRow.data('id')
                }),
                url: url,
                success: function (data) {
                    if (options.showingDeleted) {
                        jThis.attr('title', 'Error is already deleted').addClass('disabled');
                        jRow.addClass('deleted');
                        // TODO: Replace protected glyph here
                        //jCell.find('span.protected').replaceWith('<a title="Undelete and protect this error" class="protect-link" href="' + url.replace('/delete', '/protect') + '">&nbsp;P&nbsp;</a>');
                    } else {
                        var table = jRow.closest('table');
                        if (!jRow.siblings().length) {
                            $('.clear-all-div').remove();
                            $('.no-content').fadeIn('fast');
                        }
                        jRow.remove();
                        table.trigger('update', [true]);
                        refreshCounts(data);
                        $(document).trigger('exception-deleted');
                    }
                },
                error: function (xhr) {
                    jThis.attr('href', url);
                    jRow.show();
                    jCell.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred deleting');
                },
                complete: function () {
                    jThis.removeClass('icon-rotate-flip');
                }
            });
        }

        // AJAX error deletion on the list page
        // And the delete link on the detail page
        $(document).on('click', '.js-exceptions a.js-delete-link', function (e) {
            var jThis = $(this);

            // if we're already deleted, bomb out early
            if (jThis.closest('.js-error.js-deleted').length) return false;

            // if we've "protected" this error, confirm the deletion
            if (jThis.closest('tr.js-protected').length && !e.ctrlKey) {
                bootbox.confirm('Really delete this protected error?', function (result) {
                    if (result) {
                        deleteError(jThis[0]);
                    }
                });
                return false;
            }

            deleteError(this);
            return false;
        });
        // ajax the protection on the list page
        $('.js-content').on('click', '.js-exceptions a.js-protect-link', function () {
            var url = $(this).attr('href'),
                jRow = $(this).closest('tr'),
                jCell = $(this).closest('td');
            $(this).addClass('icon-rotate-flip');

            $.ajax({
                type: 'POST',
                data: $.extend({}, baseOptions, {
                    log: jRow.data('log') || options.log,
                    id: jRow.data('id')
                }),
                context: this,
                url: url,
                success: function (data) {
                    $(this).siblings('.js-delete-link').attr('title', 'Delete this error')
                        .end()
                        .replaceWith('<span class="js-protected fa fa-lock fa-fw text-primary" title="This error is protected"></span>');
                    jRow.addClass('js-protected protected').removeClass('deleted');
                    refreshCounts(data);
                },
                error: function (xhr) {
                    $(this).attr('href', url).addClass('hover-pulsate');
                    jCell.errorPopupFromJSON(xhr, 'An error occurred protecting');
                },
                complete: function () {
                    $(this).removeClass('icon-rotate-flip');
                }
            });
            return false;
        }).on('click', '.js-exceptions tbody td', function (e) {
            if ($(e.target).closest('a').length) {
                return;
            }
            var row = $(this).closest('tr');
            row.toggleClass('active warning');

            if (e.shiftKey) {
                var index = row.index(),
                    lastIndex = lastSelected.index();
                if (!e.ctrlKey) {
                    row.siblings('.active, .warning').andSelf().removeClass('active warning');
                }
                row.parent()
                    .children()
                    .slice(Math.min(index, lastIndex), Math.max(index, lastIndex)).add(lastSelected).add(row)
                    .addClass('active warning');
                if (!e.ctrlKey) {
                    lastSelected = row.first();
                }
            } else if (e.ctrlKey) {
                lastSelected = row.first();
            } else {
                if ($('.js-exceptions tbody td').length > 2) {
                    row.addClass('active warning');
                }
                row.siblings('.active, .warning').removeClass('active warning');
                lastSelected = row.first();
            }
        });

        $(document).on('keydown', function (e) {
            if (e.keyCode == 16) { // shift
                $('.js-table-exceptions').addClass('no-select');
            }
        }).on('keyup', function (e) {
            if (e.keyCode == 16) { // shift
                $('.js-table-exceptions').removeClass('no-select');
            }
        });

        $(document).keydown(function(e) {
            if (e.keyCode === 46 || e.keyCode === 8) {
                var selected = $('.js-error.active').not('.js-protected');
                if (selected.length > 0) {
                    var ids = selected.map(function () { return $(this).data('id'); }).get();
                    selected.find('.js-delete-link').addClass('icon-rotate-flip');

                    if (!options.showingDeleted) {
                        selected.hide();
                    }

                    $.ajax({
                        type: 'POST',
                        context: this,
                        traditional: true,
                        data: $.extend({}, baseOptions, {
                            ids: ids,
                            returnCounts: true
                        }),
                        url: Status.options.rootPath + 'exceptions/delete-list',
                        success: function (data) {
                            var table = selected.closest('table');
                            selected.remove();
                            table.trigger('update', [true]);
                            refreshCounts(data);
                        },
                        error: function (xhr) {
                            if (!options.showingDeleted) {
                                selected.show();
                            }
                            selected.find('.js-delete-link').removeClass('icon-rotate-flip');
                            selected.last().children().first().errorPopupFromJSON(xhr, 'An error occurred clearing selected exceptions');
                        }
                    });
                    return false;
                }
            }
            return true;
        });

        $(document).on('click', 'a.js-clear-all', function () {
            var jThis = $(this),
                id = jThis.data('id') || options.id;
            bootbox.confirm('Really delete all non-protected errors' + (id ? ' like this one' : '') + '?', function(result) {
                if (result) {
                    jThis.find('.fa').addClass('icon-rotate-flip');
                    $.ajax({
                        type: 'POST',
                        url: jThis.data('url'),
                        success: function (data) {
                            if (data.url) {
                                window.location.href = data.url;
                            } else {
                                window.location.reload(true);
                            }
                        },
                        error: function(xhr) {
                            jThis.find('.fa').removeClass('icon-rotate-flip');
                            jThis.parent().errorPopupFromJSON(xhr, 'An error occurred clearing this log');
                        }
                    });
                }
            });
            return false;
        });

        $.tablesorter.addParser({
            id: 'errorCount',
            is: function () { return false; },
            format: function (s, table, cell) {
                var count = $(cell).data('count'); // e.g. 2011-03-31 01:57:59Z
                if (!count) return 0;
                return parseInt(count, 10);
            },
            type: 'numeric'
        });

        /* Error previews */
        if (options.enablePreviews) {
            var activePreview,
                lastId,
                previewTimer = 0;

            function clearPreview(parent) {
                lastId = null;
                $('.error-preview-popup', parent).fadeOut(125, function () { $(this).remove(); });
            }

            $('.js-content').on({
                mouseenter: function (e) {
                    var jThis = $(this),
                        url = jThis.find('a').attr('href').replace('/detail', '/preview'),
                        id = jThis.closest('tr').data('id');

                    if (lastId == id) {
                        // We're moved between eye and popup
                        // Due to position: absolute, mouse events fire here, unfortunately
                        clearTimeout(previewTimer);
                        return;
                    } else {
                        lastId = id;
                    }
                    if (activePreview) {
                        activePreview.abort();
                    }

                    jThis.find('.fa').addClass('icon-rotate-flip');
                    activePreview = $.get(url, function (resp) {
                        $('.js-preview .fa.icon-rotate-flip').removeClass('icon-rotate-flip');
                        if (!$(resp).filter('.error-preview').length) return;
                        
                        var errDiv = $('<div class="error-preview-popup" />').append(resp);
                        errDiv.appendTo(jThis).fadeIn('fast');
                    });
                },
                mouseleave: function(e) {
                    if (activePreview) {
                        activePreview.abort();
                        $('.js-preview .fa.icon-rotate-flip').removeClass('icon-rotate-flip');
                    }
                    var parent = this;
                    // hack due to position: absolute firing leave even on the child popup
                    previewTimer = setTimeout(function () {
                        clearPreview(parent);
                    }, 25);
                }
            }, '.js-exceptions .js-preview');
        }

        /* Error detail handlers*/
        $(document).on('click', '.js-exception-actions a', function () {
            $(this).addClass('loading');
            $.ajax({
                type: 'POST',
                data: $.extend({}, baseOptions, {
                    id: options.id,
                    redirect: true
                }),
                context: this,
                url: $(this).data('url'),
                success: function (data) {
                    window.location.href = data.url;
                },
                error: function () {
                    $(this).removeClass('loading').parent().errorPopup('An error occured while trying to delete this error (yes, irony).');
                }
            });
            return false;
        });

        /* Jira action handlers*/
        $('.info-jira-action-link a').on('click', function () {
            $(this).addClass('loading');
            var actionid = $(this).data("actionid");
            $.ajax({
                type: 'POST',
                data: $.extend({}, baseOptions, {
                    id: options.id,
                    actionid: actionid
                }),
                context: this,
                url: Status.options.rootPath + 'exceptions/jiraaction',
                success: function (data) {
                    $(this).removeClass('loading');
                    if (data.success) {
                        if (data.browseUrl && data.browseUrl !== "") {

                            var issueLink = '<a href="' + data.browseUrl + '" target="_blank">' + data.issueKey + '</a>';
                            $("#jira-links-container").show();
                            $("#jira-links-container").append('<span> ( ' + issueLink + ' ) </span>');
                            toastr.success('<div style="margin-top:5px">' + issueLink + '</div>', 'Issue Created');
                        }
                        else {
                            toastr.success("Issue created : " + data.issueKey, 'Success');
                        }

                    }
                    else {
                        toastr.error(data.message, 'Error');
                    }

                },
                error: function () {
                    $(this).removeClass('loading').parent().errorPopup('An error occured while trying to perform the selected Jira issue action.');
                }
            });
            return false;
        });

    }

    return {
        init: init
    };
})();

Status.HAProxy = (function () {

    function init(options) {
        Status.HAProxy.options = options;

        if (options.refresh) {
            Status.Dashboard.init({ refresh: Status.HAProxy.options.refresh });
        }

        // Admin Panel (security is server-side)
        $('.js-content').on('click', '.js-haproxy-action, .js-haproxy-actions a', function (e) {
            var jThis = $(this),
                data = {
                    group: jThis.closest('[data-group]').data('group'),
                    proxy: jThis.closest('[data-proxy]').data('proxy'),
                    server: jThis.closest('[data-server]').data('server'),
                    act: jThis.data('action')
                };

            function haproxyAction() {
                jThis.find('.fa').addBack('.fa').addClass('icon-rotate-flip');
                var cog = jThis.closest('.js-dropdown-actions').find('.hover-spin > .fa').addClass('spin');
                $.ajax({
                    type: 'POST',
                    data: data,
                    url: Status.options.rootPath + 'haproxy/admin/action',
                    success: function () {
                        Status.refresh.run();
                    },
                    error: function () {
                        jThis.removeClass('icon-rotate-flip').parent().errorPopup('An error occured while trying to ' + data.act + '.');
                        cog.removeClass('spin');
                    }
                });
            }

            function confirmAction(message) {
                bootbox.confirm(message, function (result) {
                    if (result) {
                        haproxyAction();
                    }
                });
                return false;
            }
            
            if (jThis.hasClass('disabled')) return false;

            // We're at the Tier level
            if (data.group && !data.proxy && !data.server) {
                return confirmAction('Are you sure you want to ' + data.act.toLowerCase() + ' every server in <b>' + data.group + '</b>?');
            }
            // We're at the Server level
            if (!e.ctrlKey && !data.group && !data.proxy && data.server) {
                return confirmAction('Are you sure you want to ' + data.act.toLowerCase() + ' <b>' + data.server + '</b> from all backends?');
            }
            // We're at the Proxy level
            if (!e.ctrlKey && data.group && data.proxy && !data.server) {
                return confirmAction('Are you sure you want to ' + data.act.toLowerCase() + ' all of <b>' + data.proxy + '</b>?');
            }

            haproxyAction();
            return false;
        });
    }

    return {
        init: init
    };
})();

(function () {

    function bytesToSize(bytes, large, zeroLabel) {
        var sizes = large ? ['Bytes', 'KB', 'MB', 'GB', 'TB'] : ['bytes', 'kb', 'mb', 'gb', 'tb'];
        if (bytes === 0) return '0 ' + (zeroLabel || sizes[0]);
        var ubytes = Math.abs(bytes);
        var i = parseInt(Math.floor(Math.log(ubytes) / Math.log(1024)));
        var dec = (ubytes / Math.pow(1024, i)).toFixed(1).replace('.0', '');
        return dec + ' ' + sizes[i];
    }
    
    function commify(num) {
        return (num + '').replace(/(\d)(?=(\d\d\d)+(?!\d))/g, '$1,');
    }
    
    function getExtremes(array, series, min, max, stacked) {
        if (max === 'auto' || min === 'auto') {
            var maximums = { up: 0, down: 0 };
            if (stacked) {
                min = 0;
                max = d3.max(array, function (p) {
                    return d3.sum(series, function (s) { return p[s.name]; });
                });
            } else {
                series.forEach(function (s) {
                    var direction = s.direction || 'up';
                    maximums[direction] = Math.max(maximums[direction] || 0, d3.max(array, function (ss) { return ss[s.name]; }));
                });
            }
            min = min === 'auto' ? -maximums.down : min;
            max = max === 'auto' ? maximums.up : max;
        }
        return [min, max];
    }

    // Open specific methods for access
    Status.helpers = {
        bytesToSize: bytesToSize,
        commify: commify
    };

    var chartFunctions = {
        tooltipTimeFormat: d3.time.format.utc('%A, %b %d %H:%M')
    };

    var waveHtml = '<div class="sk-wave loader"><div></div><div></div><div></div><div></div><div></div></div>';

    // Creating jQuery plguins...
    $.fn.extend({
        findWithSelf: function (selector) {
            return this.find(selector).andSelf().filter(selector);
        },
        appendWaveLoader: function () {
            return this.append(waveHtml);
        },
        prependWaveLoader: function () {
            return this.prepend(waveHtml);
        },
        appendError: function(title, message) {
            return this.append('<div class="alert alert-warning"><h5>' + title + '</h5><p>' + message + '</p></div>');
        },
        prependError: function (title, message) {
            return this.prepend('<div class="alert alert-warning"><h5>' + title + '</h5><p>' + message + '</p></div>');
        },
        inlinePopup: function (className, msg, callback, removeTimeout, clickDismiss) {
            $('.' + className).remove();
            var jDiv = $('<div class="' + className + '">' + msg + '</div>').appendTo(this);
            jDiv.fadeIn('fast');
            var remove = function () { jDiv.fadeOut('fast', function () { $(this).remove(); }); if (callback) callback(); };
            if (clickDismiss) {
                jDiv.on('click', remove);
            }
            if (removeTimeout) {
                setTimeout(remove, removeTimeout);
            }
            return this;
        },
        actionPopup: function (msg, callback) {
            return this.inlinePopup('action-popup', msg, callback);
        },
        errorPopup: function (msg, callback) {
            return this.inlinePopup('error-popup', msg, callback, 15000, true);
        },
        errorPopupFromJSON: function (xhr, defaultMsg) {
            var msg = defaultMsg;
            if ((xhr && xhr.getResponseHeader('content-type') || '').indexOf('json') > -1) {
                var json = JSON.parse(xhr.responseText);
                if (json && json.ErrorMessage) {
                    msg = json.ErrorMessage;
                }
            }
            return this.errorPopup(msg);
        },
        cpuGraph: function (options) {
            return this.d3graph({
                    type: 'cpu',
                    series: [{ name: 'value', label: 'CPU' }],
                    yAxis: {
                        tickLines: true,
                        tickFormat: function (d) { return d + '%'; }
                    },
                    max: 100,
                    leftMargin: 40,
                    areaTooltipFormat: function (value) { return '<span>CPU: </span><b>' + value.toFixed(2) + '%</b>'; }
                }, options);
        },
        memoryGraph: function (options) {
            return this.d3graph({
                    type: 'memory',
                    series: [{ name: 'value', label: 'Memory' }],
                    yAxis: {
                        tickLines: true,
                        tickFormat: function (d) { return Status.helpers.bytesToSize(d * 1024 * 1024, true, 'GB'); }
                    },
                    leftMargin: 60,
                    max: this.data('max'),
                    areaTooltipFormat: function (value) { return '<span>Memory: </span><b>' + Status.helpers.bytesToSize(value * 1024 * 1024, true) + '</b>'; }
                }, options);
        },
        networkGraph: function (options) {
            return this.d3graph({
                type: 'network',
                series: [
                    { name: 'main_in', label: 'In' },
                    { name: 'main_out', label: 'Out', direction: 'down' }
                ],
                min: 'auto',
                leftMargin: 60,
                areaTooltipFormat: function(value, series, name) {
                    return '<span>Bandwidth (<span class="series-' + name + '">' + series + '</span>): </span><b>' + Status.helpers.bytesToSize(value, false) + '/s</b>';
                },
                yAxis: {
                    tickFormat: function (d) { return Status.helpers.bytesToSize(d, false); }
                }
            }, options);
        },
        volumePerformanceGraph: function (options) {
            return this.d3graph({
                type: 'volumePerformance',
                series: [
                    { name: 'main_read', label: 'Read' },
                    { name: 'main_write', label: 'Write', direction: 'down' }
                ],
                min: 'auto',
                leftMargin: 60,
                areaTooltipFormat: function (value, series, name) {
                    return '<span>I/O (<span class="series-' + name + '">' + series + '</span>): </span><b>' + Status.helpers.bytesToSize(value, false) + '/s</b>';
                },
                yAxis: {
                    tickFormat: function (d) { return Status.helpers.bytesToSize(d, false); }
                }
            }, options);
        },
        haproxyGraph: function (options) {
            var comma = d3.format(',');
            return this.d3graph({
                type: 'haproxy',
                subtype: 'traffic',
                ajaxZoom: false,
                series: [
                    { name: 'main_hits', label: 'Total' },
                    { name: 'main_pages', label: 'Pages' }
                ],
                min: 'auto',
                leftMargin: 80,
                areaTooltipFormat: function (value, series, name) { return '<span>Hits (<span class="series-' + name + '">' + series + '</span>): </span><b>' + comma(value) + '</b>'; },
                yAxis: {
                    tickFormat: comma
                }
            }, options);
        },
        haproxyRouteGraph: function (route, days, host, options) {
            return this.d3graph({
                type: 'haproxy',
                subtype: 'route-performance',
                ajaxZoom: false,
                stacked: true,
                subtitle: route,
                interpolation: 'linear',
                dateRanges: false,
                params: { route: route, days: days, host: host },
                autoColors: true,
                series: [
                    { name: 'dot_net', label: 'ASP.Net', color: '#0E2A4C' },
                    { name: 'sql', label: 'SQL', color: '#143D65' },
                    { name: 'redis', label: 'Redis', color: '#194D79' },
                    { name: 'http', label: 'HTTP', color: '#1D5989' },
                    { name: 'tag_engine', label: 'Tag Engine', color: '#206396' },
                    { name: 'other', label: 'Other', color: '#64B6D0' }
                ],
                rightSeries: [
                    { name: 'hits', label: 'Hits', color: 'rgb(116, 196, 118)', width: 2 }
                ],
                rightMargin: 70,
                min: 'auto',
                leftMargin: 60,
                // TODO: Style except for BG color in .less
                rightAreaTooltipFormat: function (value, series, name, color) {
                    return '<label>' + (color ? '<div style="background-color: ' + color + '; width: 16px; height: 13px; display: inline-block;"></div> ' : '')
                        + '<span class="series-' + name + '">' + series + '</span>: </label><b>' + Status.helpers.commify(value) + '</b>';
                },
                areaTooltipFormat: function (value, series, name, color) {
                    return '<label>' + (color ? '<div style="background-color: ' + color + '; width: 16px; height: 13px; display: inline-block;"></div> ' : '')
                        + '<span class="series-' + name + '">' + series + '</span>: </label><b>' + Status.helpers.commify(value) + ' <span class="text-muted">ms</span></b>';
                },
                yAxis: {
                    tickFormat: function (d) { return Status.helpers.commify(d) + ' ms'; }
                }
            }, options);
        },
        
        d3graph: function (options, addOptions) {
            var defaults = {
                series: [{ name: 'value', label: 'Main' }],
                ajaxZoom: true,
                stacked: false,
                autoColors: false,
                dateRanges: true,
                interpolation: 'linear',
                leftMargin: 40,
                live: false,
                id: this.data('id'),
                title: this.data('title'),
                subtitle: this.data('subtitle'),
                start: this.data('start'),
                end: this.data('end'),
                width: 'auto',
                height: 'auto',
                max: 'auto',
                min: 0
            };
            options = $.extend(true, {}, defaults, options, addOptions);

            this.addClass('chart');

            var minDate, maxDate,
                topBrushArea, bottomBrushArea,
                dataLoaded,
                curWidth, curHeight, curData,
                chart = this,
                buildTooltip = $('<div class="build-tooltip chart-tooltip small" />').appendTo(chart),
                areaTooltip = $('<div class="area-tooltip chart-tooltip small" />').appendTo(chart),
                series = options.series,
                rightSeries = options.rightSeries,
                leftPalette = options.autoColors === true ? options.leftPalette || 'PuBu' : (options.autoColors || null),
                rightPalette = options.autoColors === true ? options.rightPalette || 'Greens' : (options.rightPalette || options.autoColors || null),
                margin, margin2, width, height, height2,
                x, x2, y, yr, y2,
                xAxis, xAxis2, yAxis, yrAxis,
                brush, brush2,
                clipId = 'clip' + Status.graphCount++,
                gradientId = 'gradient-' + Status.graphCount,
                svg, focus, context, clip,
                currentArea,
                refreshTimer,
                urlPath = Status.options.rootPath + 'graph/' + options.type + (options.subtype ? '/' + options.subtype : '') + '/json',
                params = { summary: true },
                curDataRequest,
                stack, stackArea, stackSummaryArea, stackFunc; // stacked specific vars
            
            if (options.id) params.id = options.id;
            if (options.iid) params.iid = options.iid;
            if (options.start) params.start = (new Date(options.start).getTime() / 1000).toFixed(0);
            if (options.end) params.end = (new Date(options.end).getTime() / 1000).toFixed(0);
            $.extend(params, options.params);
            
            options.width = options.width === 'auto' ? (chart.width() - 10) : options.width;
            options.height = options.height === 'auto' ? (chart.height() - 5) : options.height;

            if (options.title) {
                var titleDiv = $('<div class="chart-title"/>').text(options.title).prependTo(chart);
                if (options.subtitle) {
                    $('<div class="chart-subtitle"/>').text(options.subtitle).appendTo(titleDiv);
                }
            }
            
            function drawElements() {
                if (options.width - 10 - options.leftMargin < 300)
                    options.width = 300 + 10 + options.leftMargin;
                
                margin = { top: 10, right: options.rightMargin || 10, bottom: options.live ? 25 : 100, left: options.leftMargin };
                width = options.width - margin.left - margin.right;
                height = options.height - margin.top - margin.bottom;

                var timeFormats = d3.time.format.utc.multi([
                    ['.%L', function (d) { return d.getUTCMilliseconds(); }],
                    [':%S', function (d) { return d.getUTCSeconds(); }],
                    ['%H:%M', function (d) { return d.getUTCMinutes(); }],
                    ['%H:%M', function (d) { return d.getUTCHours(); }],
                    ['%a %d', function (d) { return d.getUTCDay() && d.getUTCDate() !== 1; }],
                    ['%b %d', function (d) { return d.getUTCDate() !== 1; }],
                    ['%B', function (d) { return d.getUTCMonth(); }],
                    ['%Y', function () { return true; }]
                ]);

                x = d3.time.scale.utc().range([0, width]);
                y = d3.scale.linear().range([height, 0]);
                yr = d3.scale.linear().range([height, 0]);


                xAxis = d3.svg.axis().scale(x).orient('bottom').tickFormat(timeFormats);
                yAxis = d3.svg.axis().scale(y).orient('left');
                yrAxis = d3.svg.axis().scale(yr).orient('right');
                
                if (options.yAxis) {
                    if (options.yAxis.tickFormat) {
                        yAxis.tickFormat(options.yAxis.tickFormat);
                    }
                    if (options.yAxis.tickSize) {
                        yAxis.tickSize(options.yAxis.tickSize);
                    }
                    if (options.yAxis.tickLines) {
                        yAxis.tickSize(-width);
                    }
                }

                brush = d3.svg.brush()
                    .x(x)
                    .on('brushend', redrawFromMain);

                
                svg = d3.select(chart[0]).append('svg')
                    .attr('width', options.width)
                    .attr('height', options.height);

                clip = svg.append('defs').append('clipPath')
                    .attr('id', clipId)
                    .append('rect')
                    .attr('width', width)
                    .attr('height', height);

                focus = svg.append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');


                if (!options.live) { // Summary elements
                    margin2 = { top: options.height - 77, right: 10, bottom: 20, left: options.leftMargin };
                    height2 = options.height - margin2.top - margin2.bottom;
                    x2 = d3.time.scale.utc().range([0, width]);
                    y2 = d3.scale.linear().range([height2, 0]);
                    xAxis2 = d3.svg.axis().scale(x2).orient('bottom').tickFormat(timeFormats);

                    brush2 = d3.svg.brush()
                        .x(x2)
                        .on('brush', redrawFromSummary);

                    context = svg.append('g').attr('transform', 'translate(' + margin2.left + ',' + margin2.top + ')');
                }
            }
            
            function getClass(prefix, s) {
                return function (a) {
                    return prefix + ' ' + ('series-' + (s || a).name) + ((s || a).cssClass ? ' ' + (s || a).cssClass : '');
                };
            }

            function getColor(pSeries, palette, inverse) {
                pSeries = pSeries || series;
                palette = palette || leftPalette;
                // TODO: Move this up, no need to re-run on hover
                
                if (!palette) return null;
                
                var colors = colorbrewer[palette];
                if (!colors) return null;
                
                var cPalette = colors[pSeries.length < 3 ? 3 : pSeries.length > 9 ? 9 : pSeries.length];
                
                return function (a, i) {
                    return pSeries[i].color || cPalette[inverse ? cPalette.length - 1 - i : i];
                };
            }
            
            function drawPrimaryGraphs(data) {
                x.domain(options.ajaxZoom ? d3.extent(data.points.map(function (d) { return d.date; })) : [minDate, maxDate]);

                if (!options.live) { // Summary
                    x2.domain(d3.extent(data.summary.map(function(d) { return d.date; })));
                    y2.domain(getExtremes(data.summary, series, options.min, options.max));
                }

                rescaleYAxis(data, true);
                
                if (options.stacked) {
                    stack = d3.layout.stack().values(function (d) { return d.values; });
                    stackArea = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function(d) { return x(d.date); })
                        .y0(function(d) { return y(d.y0); })
                        .y1(function (d) { return y(d.y0 + d.y); });
                    if (!options.live) { // Summary
                        stackSummaryArea = d3.svg.area()
                            .x(function(d) { return x2(d.date); })
                            .y0(function(d) { return y2(d.y0); })
                            .y1(function(d) { return y2(d.y0 + d.y); });
                    }
                    stackFunc = function(dataList) {
                        return stack(series.map(function(s) {
                            var result = { name: s.name, values: dataList.map(function (d) { return { date: d.date, y: d[s.name] }; }) };
                            if (s.cssClass) result.cssClass = s.cssClass;
                            if (s.color) result.color = s.color;
                            return result;
                        }));
                    };

                    focus.selectAll('.area')
                        .data(stackFunc(data.points))
                        .enter()
                        .append('path')
                        .attr('class', getClass('area'))
                        .attr('fill', getColor())
                        .attr('clip-path', 'url(#' + clipId + ')')
                        .attr('d', function (d) { return stackArea(d.values); });

                    context.selectAll('.area')
                        .data(stackFunc(data.summary))
                        .enter()
                        .append('path')
                        .attr('class', getClass('summary-area'))
                        .attr('fill', getColor())
                        .attr('d', function (d) { return stackSummaryArea(d.values); });
                } else {
                    // draw a path for each series in the main graph
                    series.forEach(function (s) {
                        focus.append('path')
                            .datum(data.points)
                            .attr('class', getClass('area', s))
                            .attr('fill', options.colorStops ? 'url(#' + gradientId + ')' : getColor())
                            .attr('clip-path', 'url(#' + clipId + ')')
                            .attr('d', s.area.y0(y(0)));
                        if (!options.live) {
                            // and the summary
                            context.append('path')
                                .datum(data.summary)
                                .attr('class', getClass('summary-area', s))
                                .attr('fill', getColor())
                                .attr('d', s.summaryArea.y0(y2(0)));
                        }
                    });
                }

                if (options.rightSeries) {
                    rightSeries.forEach(function (s) {
                        var line = focus.append('path')
                            .datum(data.summary)
                            .attr('class', getClass('line', s, series.length))
                            .attr('fill', 'none')
                            .attr('stroke', getColor(rightSeries, rightPalette))
                            .attr('clip-path', 'url(#' + clipId + ')')
                            .attr('d', s.line);
                        if (s.width) {
                            line.attr('stroke-width', s.width);
                        }
                    });
                }

                // x-axis
                focus.append('g')
                    .attr('class', 'x axis')
                    .attr('transform', 'translate(0,' + height + ')')
                    .call(xAxis);
                // y-axis
                focus.append('g')
                    .attr('class', 'y axis')
                    .call(yAxis);
                // right-hand y-axis
                if (rightSeries) {
                    focus.append('g')
                        .attr('class', 'yr axis')
                        .attr('transform', 'translate(' + width + ', 0)')
                        .call(yAxis);
                }

                rescaleYAxis(data, false);
                
                // current hover area
                currentArea = focus.append('g')
                    .attr('class', 'current-area')
                    .style('display', 'none');
                // hover line
                currentArea.append('svg:line')
                    .attr('x1', 0)
                    .attr('x2', 0)
                    .attr('y1', height + margin.top)
                    .attr('y2', margin.top)
                    .attr('class', 'area-tooltip-line');
                // hover circle(s)
                if (options.stacked) {
                    currentArea.selectAll('circle')
                        .data(stackFunc(data.summary))
                        .enter()
                        .append('circle')
                        .attr('class', getClass(''))
                        .attr('fill', getColor())
                        .attr('r', 4.5);
                } else {
                    series.forEach(function (s) {
                        currentArea.append('circle')
                            .attr('class', getClass('', s))
                            .attr('fill', getColor())
                            .attr('r', 4.5);
                    });
                }
                if (rightSeries) {
                    rightSeries.forEach(function (s) {
                        currentArea.append('circle')
                            .attr('class', getClass('', s))
                            .attr('fill', getColor())
                            .attr('r', 4.5);
                    });
                }

                // top selection brush, for main graph
                topBrushArea = focus.append('g')
                    .attr('class', 'x brush')
                    .on('mousemove', areaHover)
                    .on('mouseover', areaEnter)
                    .on('mouseout', areaLeave)
                    .call(brush);
                topBrushArea.selectAll('rect')
                    .attr('y', 0)
                    .attr('height', height + 1);

                if (!options.live) {
                    context.append('g')
                        .attr('class', 'x axis')
                        .attr('transform', 'translate(0,' + height2 + ')')
                        .call(xAxis2);

                    // bottom selection brush, for summary
                    bottomBrushArea = context.append('g')
                        .attr('class', 'x brush')
                        .call(brush2);
                    bottomBrushArea.selectAll('rect')
                        .attr('y', -6)
                        .attr('height', height2 + 7);
                }


                curWidth = chart.width();
                curHeight = chart.height();
            }

            function drawBuilds(data) {
                if (!data.builds) return;
                
                focus.append('svg:g')
                    .attr('class', 'build-dots')
                    .selectAll('scatter-dots')
                    .data(data.builds)
                    .enter().append('svg:circle')
                    .attr('class', 'build-dot')
                    .attr('cy', y(0))
                    .attr('cx', function (d) { return x(d.date); })
                    .attr('r', 6)
                    .style('opacity', 0.6)
                    .on('mouseover', function (d) {
                        var pos = $(this).position();
                        buildTooltip.html('<span class="label">Build: </span>' + d.text)
                            .css({ left: pos.left - (buildTooltip.width() / 2), top: pos.top + 25 })
                            .stop(true, true)
                            .fadeIn(200);
                    })
                    .on('mouseout', function () {
                        buildTooltip.stop(true, true).fadeOut(200);
                    })
                    .on('click', function () {
                        window.open(data.link, '_blank');
                    });
            }

            function clearBuilds() {
                focus.selectAll('.build-dots').remove();
                focus.selectAll('.build-dot').remove();
            }
            
            function prepSeries() {
                series.forEach(function (s) {
                    var negative = s.direction === 'down';
                    s.area = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function (d) { return x(d.date); })
                        .y1(function (d) { return y(negative ? -d[s.name] : d[s.name]); });
                    s.summaryArea = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function (d) { return x2(d.date); })
                        .y1(function(d) { return y2(negative ? -d[s.name] : d[s.name]); });
                });
                if (rightSeries) {
                    rightSeries.forEach(function (s) {
                        var negative = s.direction === 'down';
                        s.line = d3.svg.line()
                            .interpolate(options.interpolation)
                            .x(function (d) { return x(d.date); })
                            .y(function (d) { return yr(negative ? -d[s.name] : d[s.name]); });
                    });
                }
            }
            
            function rescaleYAxis(data, rescale) {
                if (rescale) {
                    y.domain(getExtremes(data.points, series, options.min, options.max, options.stacked));
                    if (rightSeries) {
                        yr.domain(getExtremes(data.points, rightSeries, 'auto', 'auto'));
                    }
                }
                focus.select('g.y.axis')
                    .call(yAxis)
                    .selectAll('g.tick.major line')
                    .attr('x1', width);
                if (rightSeries) {
                    focus.select('g.yr.axis')
                        .call(yrAxis)
                        .selectAll('g.tick.major line')
                        .attr('x1', width);
                }
            }
            
            function areaEnter() {
                if (curData && curData.points && curData.points.length) {
                    currentArea.style('display', null);
                    areaTooltip.show();
                }
            }
            
            function areaLeave() {
                currentArea.style('display', 'none');
                areaTooltip.hide();
            }

            function areaHover() {
                // no data! what the hell are you trying to hover?
                if (!dataLoaded) return;

                var pos = d3.mouse(this),
                    date = x.invert(pos[0]),
                    bisector = d3.bisector(function(d) { return d.date; }).left,
                    tooltip = '<div class="tooltip-date">' + chartFunctions.tooltipTimeFormat(date) + ' <span class="text-muted">UTC</span></div>',
                    data = options.ajaxZoom ? curData.points : curData.summary,
                    index = bisector(data, date, 1), // bisect the curData array to get the index of the hovered date
                    dateBefore = data[Math.max(index - 1, 0)], // get the date before the hover
                    dateAfter = data[Math.min(index, data.length - 1)], // and the date after
                    d = dateBefore && date - dateBefore.date > dateAfter.date - date ? dateAfter : dateBefore, // pick the nearest
                    through = dateBefore && (date.getTime() - dateBefore.date.getTime()) / (dateAfter.date.getTime() - dateBefore.date.getTime()),
                    tooltipRows = [];
                
                if (!d) {
                    currentArea.style('display', 'none');
                    return;
                }

                // align the moons! or at least the series hover dots
                var runningTotal = 0;
                if (rightSeries) {
                    rightSeries.forEach(function (s, i) {
                        var val = d[s.name] || 0, gc = getColor(rightSeries, rightPalette),
                        fakeVal = options.interpolation === 'linear'
                            ? d3.interpolate(dateBefore[s.name], dateAfter[s.name])(through)
                            : val,
                            cPos = (s.direction === 'down' ? -1 : 1) * fakeVal;
                        tooltip += (options.rightAreaTooltipFormat || areaTooltipFormat)(val, s.label, s.name, gc ? gc(s, i) : null);
                        currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + yr(cPos) + ')');
                    });
                }
                
                series.forEach(function (s, i) {
                    var val = d[s.name] || 0, gc = getColor(),
                        fakeVal = options.interpolation === 'linear'
                            ? d3.interpolate(dateBefore[s.name], dateAfter[s.name])(through)
                            : val;
                    runningTotal += fakeVal;
                    var cPos = (s.direction === 'down' ? -1 : 1)
                        * (options.stacked ? runningTotal : fakeVal);

                    tooltipRows.push(options.areaTooltipFormat(val, s.label, s.name, gc ? gc(s, i) : null));
                    currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + y(cPos) + ')');
                });

                if (options.stacked) {
                    tooltipRows.reverse();
                }
                tooltip += tooltipRows.join('');

                areaTooltip.html(tooltip)
                    .css({ left: pos[0] + 80, top: pos[1] + 60 });

                currentArea.attr('transform', 'translate(' + (pos[0]) + ', 0)');
            }

            function onWindowResized() {
                var newWidth = chart.width(),
                    newHeight = chart.height();
                if (curWidth !== newWidth || curHeight !== newHeight) {
                    options.width = curWidth = newWidth;
                    curHeight = newHeight;

                    if (dataLoaded) {
                        //TODO: Chart re-use by resize, with transform
                        svg.remove();
                        drawElements();
                        drawPrimaryGraphs(curData);
                        drawBuilds(curData);
                    }
                }
            }

            $(window).on('resize', onWindowResized);

            // lay it all out soon as possible
            drawElements();

            function handleData(data) {
                postProcess(data);
                prepSeries();
                drawPrimaryGraphs(data);
                drawBuilds(data);
                dataLoaded = true;
                chart.find('.loader').hide();

                if (!options.live) {
                    // set the initial summary brush to reflect what was loaded up top
                    brush2.extent(x.domain())(bottomBrushArea);
                }

                if (options.showBuilds && !data.builds) {
                    //$.getJSON(Status.options.rootPath + 'graph/builds/json', params, function (bData) {
                    //    postProcess(bData);
                    //    drawBuilds(bData);
                    //});
                }
            }

            if (options.data) {
                handleData(options.data);
            } else {
                $.getJSON(urlPath, params)
                    .done(handleData)
                    .fail(function () {
                        chart.prependError('Error', 'Could not load graph');
                    });
            }

            function postProcess(data) {
                function process(name) {
                    if (data[name]) {
                        data[name].forEach(function(d) {
                            d.date = new Date(d.date * 1000);
                        });
                        curData[name] = data[name];
                    }
                }

                if (!options.ajaxZoom && !data.points)
                    data.points = data.summary;

                if (!curData) curData = {};
                process('points');
                process('summary');
                process('builds');
                
                if (data.summary) {
                    if (data.summary.length >= 2) {
                        minDate = data.summary[0].date;
                        maxDate = data.summary[data.summary.length - 1].date;
                    }
                }
            }

            function redrawFromMain() {
                var bounds = brush.empty() ? x.domain() : brush.extent();
                brush.clear()(topBrushArea);
                brush2.extent(bounds)(bottomBrushArea);
                redrawMain(bounds, 1);
            }

            function redrawFromSummary() {
                redrawMain(brush2.empty() ? x2.domain() : brush2.extent());
            }

            function redrawMain(newBounds, timerDelay) {
                var start = Math.round(newBounds[0] / 1000),
                    end = Math.round(newBounds[1] / 1000);

                // load low-res summary view quickly
                if (options.ajaxZoom) {
                    if (options.stacked) {
                        focus.selectAll('.area')
                            .datum(curData.summary);
                    } else {
                        //TODO: This is probably broken
                        focus.selectAll('path.area')
                            .datum(curData.summary);
                    }
                    if (rightSeries) {
                        rightSeries.forEach(function (s) {
                            focus.append('path')
                                .datum(data.points)
                                .attr('d', s.line);
                        });
                    }
                }
                // set the new bounds from the summary selection
                x.domain(newBounds);
                clearBuilds();

                if (options.ajaxZoom) {
                    //refresh with high-res goodness
                    clearTimeout(refreshTimer);
                    refreshTimer = setTimeout(function () {
                        if (curDataRequest) {
                            curDataRequest.abort();
                        }
                        curDataRequest = $.getJSON(urlPath, { id: options.id, start: start, end: end }, function (newData) {
                            postProcess(newData);
                            rescaleYAxis(newData, true);
                            series.forEach(function (s) {
                                focus.select('path.area.series-' + s.name)
                                    .datum(newData.points)
                                    .attr('d', s.area.y0(y(0)))
                                    .attr('fill-opacity', 1);
                            });
                            drawBuilds(newData);
                        });
                    }, timerDelay || 50);
                } else {
                    curData.points = curData.summary.filter(function (p) {
                        var t = p.date.getTime();
                        return start <= t && t <= end;
                    });
                    rescaleYAxis(curData, true);
                }

                // redraw
                if (options.stacked) {
                    focus.selectAll('.area').attr('d', function(d) {
                        return stackArea(d.values);
                    });
                } else {
                    series.forEach(function (s) {
                        focus.select('path.area.series-' + s.name)
                            .attr('d', s.area.y0(y(0)));
                    });
                }
                if (rightSeries) {
                    rightSeries.forEach(function (s) {
                        focus.select('path.line.series-' + s.name).attr('d', s.line);
                    });
                }
                focus.select('.x.axis').call(xAxis);
            }
            
            return this
                .removeClass('cpu-chart memory-chart network-chart volumePerformance-chart')
                .addClass(options.type + (options.subtype ? '-' + options.subtype : '') + '-chart');
        },
        lived3graph: function(options) {
            var defaults = {
                series: [{ name: 'value', label: 'Main' }],
                leftMargin: 40,
                id: this.data('id'),
                start: this.data('start'),
                end: this.data('end'),
                title: this.data('title'),
                subtitle: this.data('subtitle'),
                slideDurationMs: 1000,
                durationSeconds: 5 * 60,
                width: 660,
                height: 300,
                max: 'auto',
                min: 0
            };
            options = $.extend({}, defaults, options);

            var curWidth, curHeight,
                now = new Date(),
                series = options.series,
                curData = d3.range(60 * 10).map(function(i) {
                    var result = { date: new Date(+now - (60 * 10 * 1000) + (i * 1000)) };
                    series.forEach(function (s) {
                        result[s.name] = i === 60 * 10 ? 0 : null;
                    });
                    return result;
                }),
                chart = this,
                areaTooltip = $('<div class="area-tooltip chart-tooltip" />').appendTo(chart),
                margin, width, height,
                x, y, xAxis, yAxis,
                svg, focus, clip,
                clipId = 'clip' + Status.graphCount++,
                currentArea,
                urlPath = '/dashboard/node/poll/' + options.type + (options.subtype ? '/' + options.subtype : ''),
                params = $.extend({}, { id: options.id, start: options.start / 1000, end: options.end / 1000 }, options.params);

            options.width = options.width === 'auto' ? (chart.width() - 10) : options.width;
            options.height = options.height === 'auto' ? (chart.height() - 40) : options.height;

            if (options.title) {
                var titleDiv = $('<div class="chart-title"/>').text(options.title).prependTo(chart);
                if (options.subtitle) {
                    $('<div class="chart-subtitle"/>').text(options.subtitle).appendTo(titleDiv);
                }
            }
            
            function drawElements() {
                if (options.width - 10 - options.leftMargin < 300)
                    options.width = 300 + 10 + options.leftMargin;
                
                margin = { top: 10, right: 10, bottom: 20, left: options.leftMargin };
                width = options.width - margin.left - margin.right;
                height = options.height - margin.top - margin.bottom;

                x = d3.time.scale.utc().range([0, width]);
                y = d3.scale.linear().range([height, 0]);

                xAxis = d3.svg.axis().scale(x).orient('bottom');
                yAxis = d3.svg.axis().scale(y).orient('left');

                if (options.yAxis) {
                    if (options.yAxis.tickFormat) {
                        yAxis.tickFormat(options.yAxis.tickFormat);
                    }
                    if (options.yAxis.tickSize) {
                        yAxis.tickSize(options.yAxis.tickSize);
                    }
                }
                
                svg = d3.select(chart[0]).append('svg')
                    .attr('width', options.width)
                    .attr('height', options.height);

                clip = svg.append('defs').append('clipPath')
                    .attr('id', clipId)
                    .append('rect')
                    .attr('width', width)
                    .attr('height', height);

                focus = svg.append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');
            }
            
            function drawPrimaryGraphs(data) {
                rescaleYAxis(data, true);
                
                // draw a path for each series in the main graph
                series.forEach(function (s) {
                    focus.append('path')
                        .datum(data)
                        .attr('class', 'area series-' + s.name)
                        .attr('clip-path', 'url(#' + clipId + ')')
                        .attr('d', s.area.y0(y(0)));
                });

                // top hover brush, for main graph
                focus.append('g')
                    .attr('class', 'x brush')
                    .on('mousemove', areaHover)
                    .on('mouseover', areaEnter)
                    .on('mouseout', areaLeave)
                    .call(d3.svg.brush().x(x))
                    .selectAll('rect')
                    .attr('y', 0)
                    .attr('height', height + 1);

                // x-axis
                focus.append('g')
                    .attr('class', 'x axis')
                    .attr('transform', 'translate(0,' + height + ')')
                    .call(xAxis);
                // y-axis
                focus.append('g')
                    .attr('class', 'y axis')
                    .call(yAxis);

                rescaleYAxis(data, false);
                
                // current hover area
                currentArea = focus.append('g')
                    .attr('class', 'current-area')
                    .style('display', 'none');
                // hover line
                currentArea.append('svg:line')
                    .attr('x1', 0)
                    .attr('x2', 0)
                    .attr('y1', height + margin.top)
                    .attr('y2', margin.top)
                    .attr('class', 'area-tooltip-line');
                // hover circle(s)
                series.forEach(function (s) {
                    currentArea.append('circle')
                        .attr('class', 'series-' + s.name)
                        .attr('r', 4.5);
                });

                curWidth = chart.width();
                curHeight = chart.height();
            }
            
            function prepSeries() {
                series.forEach(function (s) {
                    var negative = s.direction === 'down';
                    s.area = d3.svg.area()
                        .interpolate('basis')
                        .x(function (d) { return x(d.date); })
                        .y1(function (d) { return y(negative ? -d[s.name] : d[s.name]); });
                });
            }
            
            function rescaleYAxis(data, rescale) {
                if(rescale) y.domain(getExtremes(data, series, options.min, options.max));
                focus.select('g.y.axis')
                    .call(yAxis)
                    .selectAll('g.tick.major line')
                    .attr('x1', width);
            }
            
            function areaEnter() {
                currentArea.style('display', null);
                areaTooltip.show();
            }
            
            function areaLeave() {
                currentArea.style('display', 'none');
                areaTooltip.hide();
            }

            function areaHover() {
                var pos = d3.mouse(this),
                    date = x.invert(pos[0]),
                    bisector = d3.bisector(function (d) { return d.date; }).left,
                    tooltip = '<div class="tooltip-date">' + chartFunctions.tooltipTimeFormat(date) + ' <span class="text-muted">UTC</span></div>';

                // align the moons! or at least the series hover dots
                series.forEach(function (s) {
                    var data = curData,
                        index = bisector(data, date, 1), // bisect the curData array to get the index of the hovered date
                        dateBefore = data[index - 1],    // get the date before the hover
                        dateAfter = data[index],         // and the date after
                        d = dateBefore && date - dateBefore.date > dateAfter && dateAfter.date - date ? dateAfter : dateBefore; // pick the nearest

                    tooltip += options.areaTooltipFormat(d[s.name], s.label, s.name);
                    currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + y(s.direction === 'down' ? -d[s.name] : d[s.name]) + ')');
                });
                
                areaTooltip.html(tooltip)
                    .css({ left: pos[0] - (areaTooltip.width() / 2), top: pos[1] - areaTooltip.height() - 20 });

                currentArea.attr('transform', 'translate(' + (pos[0]) + ', 0)');
            }

            function onWindowResized() {
                var newWidth = chart.width(),
                    newHeight = chart.height();
                if (curWidth !== newWidth || curHeight !== newHeight) {
                    options.width = curWidth = newWidth;
                    curHeight = newHeight;

                    svg.remove();
                    drawElements();
                    drawPrimaryGraphs(curData);
                }
            }

            $(window).on('resize', onWindowResized);

            // lay it all out soon as possible
            drawElements();
            var start = new Date();
            x.domain([start - (options.durationSeconds - 2) * options.slideDurationMs, start - options.slideDurationMs]);

            var curValue = { total: options.startValue || 0 };
            
            // should be passed a { date: dateVal, series1: value, series2: value } style object
            function tick() {

                // update the domains
                var toInsert = { total: curValue.total };
                now = new Date();
                toInsert.date = now;
                x.domain([now - options.durationSeconds * 1000 - options.slideDurationMs, now - options.slideDurationMs]);

                // push the accumulated count onto the back, and reset the count
                curData.push(toInsert);
                curData.shift();

                // redraw the areas
                series.forEach(function (s) {
                    focus.select('path.series-' + s.name)
                        .attr('d', s.area)
                        .attr('transform', null);
                    focus.select('path.series-' + s.name)
                        .transition()
                        .duration(options.slideDurationMs)
                        .ease('linear')
                        .attr('transform', 'translate(' + x(now - options.durationSeconds * 1000) + ')');
                });
                
                // slide the x-axis left
                focus.select('g.x.axis')
                    .transition()
                    .duration(options.slideDurationMs)
                    .ease('cubic-in-out')
                    .call(xAxis);
            }
            
            prepSeries();
            drawPrimaryGraphs(curData);


            var ticker = setInterval(tick, options.slideDurationMs),
                abort = false;

            var dataPoll = function() {
                $.getJSON(urlPath, params, function (data) {
                    curValue = data;
                    if (!abort) setTimeout(dataPoll, options.slideDurationMs);
                });
            };
            dataPoll();

            this.removeClass('cpu-chart memory-chart network-chart volumePerformance-chart')
                .addClass(options.type + (options.subtype ? '-' + options.subtype : '') + '-chart');

            function stop() {
                clearInterval(ticker);
                abort = true;
            }

            return {
                tick: tick,
                stop: stop
            };
        }
    });
})();