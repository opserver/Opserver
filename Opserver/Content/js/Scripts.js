if (window.devicePixelRatio >= 2) {
    Cookies.set('highDPI', 'true', { expires: 365 * 10, path: '/' });
}

window.Status = (function () {

    var ajaxLoaders = {},
        registeredRefreshes = {};

    function registerRefresh(name, callback, interval, paused) {
        var refreshData = {
            name: name,
            func: callback,
            interval: interval,
            paused: paused // false on init almost always
        };
        registeredRefreshes[name] = refreshData;
        refreshData.timer = setTimeout(function() { execRefresh(refreshData); }, refreshData.interval);
    }

    function runRefresh(name) {
        pauseRefresh(name);
        resumeRefresh(name);
    }

    function execRefresh(refreshData) {
        if (refreshData.paused) {
            return;
        }
        var def = refreshData.func();
        if (typeof (def.done) === "function") {
            def.done(function () {
                refreshData.timer = setTimeout(function () { execRefresh(refreshData); }, refreshData.interval);
            });
        }

        refreshData.running = def;
        refreshData.timer = 0;
    }

    function pauseRefresh(name) {

        function pauseSingleRefresh(r) {
            r.paused = true;
            if (r.timer) {
                clearTimeout(r.timer);
                r.timer = 0;
            }
            if (r.running) {
                if (typeof (r.running.reject) === "function") {
                    r.running.reject();
                }
                if (typeof (r.running.abort) === "function") {
                    r.running.abort();
                }
            }
        }

        if (name && registeredRefreshes[name]) {
            console.log('Refresh paused for: ' + name);
            pauseSingleRefresh(registeredRefreshes[name]);
            return;
        }

        console.log('Refresh paused');
        for (var key in registeredRefreshes) {
            if (registeredRefreshes.hasOwnProperty(key)) {
                pauseSingleRefresh(registeredRefreshes[key]);
            }
        }
    }

    function resumeRefresh(name) {

        function resumeSingleRefresh(r) {
            if (r.timer) {
                clearTimeout(r.timer);
            }
            r.paused = false;
            execRefresh(r);
        }

        if (name && registeredRefreshes[name]) {
            console.log('Refresh resumed for: ' + name);
            resumeSingleRefresh(registeredRefreshes[name]);
            return;
        }

        console.log('Refresh resumed');
        for (var key in registeredRefreshes) {
            if (registeredRefreshes.hasOwnProperty(key)) {
                resumeSingleRefresh(registeredRefreshes[key]);
            }
        }
    }

    function summaryPopup(url, options, noClose, onClose) {
        var wrap = getPopup(noClose);
        wrap.load(url, options, function () {
            // TODO: refresh intervals via header
            showSummaryPopup(onClose, 50);
        });
    }

    function getPopup(noClose) {
        if (noClose) {
            var current = $('.summary-popup:visible');
            if (current.length) return current;
        }
        $.modal.close();
        $('.summary-popup').remove();
        return $('<div class="summary-popup" />').appendTo('#content');
    }

    function showSummaryPopup(onClose) {
        $('.summary-popup').modal({
            overlayClose: true,
            autoResize: true,
            maxWidth: '95%',
            maxHeight: '95%', // height < 840 ? height - 40 : 800,
            containerCss: { 'height': 'auto', 'width': 'auto' },
            onShow: function (dialog) {
                dialog.wrap.css('overflow', 'hidden');
            },
            onClose: function (dialog) {
                if (onClose) onClose();
                dialog.data.fadeOut('fast', function () {
                    dialog.container.hide('fast', function () {
                        $.modal.close();
                    });
                });
                window.location.hash = '';
            }
        });
        resizePopup();
    }

    function resizePopup() {
        var container = $('#simplemodal-container');
        if (!container.length) return;

        var active = $('.sql-bottom-active');
        if (!active.length) {
            $(window).trigger('resize.simplemodal');
            return;
        }

        container.css('height', 'auto');
        active.css({ 'overflow-y': 'visible', 'height': 'auto' });

        var maxHeight = $(window).height() * 0.90;
        if (container.height() > maxHeight) {
            container.css('height', maxHeight + 'px');
        }

        var visibleHeight = $('.simplemodal-wrap').height() - active.position().top;
        if (active[0].scrollHeight > visibleHeight) {
            active.css({ 'overflow-y': 'scroll', 'height': visibleHeight - 5 + 'px' });
        } else {
            active.css({ 'overflow-y': 'visible', 'height': visibleHeight + 'px' });
        }
        $.modal.setPosition();

        return;
    }
    
    function hashChangeHandler() {
        var hash = window.location.hash;
        if (!hash || hash.length > 1) {
            for (var h in ajaxLoaders) {
                if (ajaxLoaders.hasOwnProperty(h) && hash.indexOf(h) == 0) {
                    var val = hash.replace(h, '');
                    ajaxLoaders[h](val);
                }
            }
        }
    }

    $(window).on('hashchange', hashChangeHandler).on('resize', resizePopup);
    $(function () {
        // individual sections add ajaxLoaders, delay running until after they're added on-load
        setTimeout(hashChangeHandler, 1);
    });
    
    function prepTableSorter() {
        $.tablesorter.addParser({
            id: 'relativeDate',
            is: function () { return false; },
            format: function (s, table, cell) {
                var date = $(cell).find('.relativetime').attr('title'); // e.g. 2011-03-31 01:57:59Z
                if (!date)
                    return 0;

                var exp = /(\d{4})-(\d{1,2})-(\d{1,2})\W*(\d{1,2}):(\d{1,2}):(\d{1,2})Z/i.exec(date);
                return new Date(exp[1], exp[2], exp[3], exp[4], exp[5], exp[6], 0).getTime();
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'commas',
            is: function () { return false; },
            format: function (s) {
                return s.replace('$', '').replace(/,/g, '');
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'dataVal',
            is: function () { return false; },
            format: function (s, table, cell) {
                return $(cell).data('val') || 0;
            },
            type: 'numeric'
        });
        $.tablesorter.addParser({
            id: 'cellText',
            is: function () { return false; },
            format: function (s, table, cell) {
                return $(cell).text();
            },
            type: 'text'
        });
    }

    function init(options) {
        Status.options = options;

        if (options.HeaderRefresh) {
            Status.refresh.register("TopBar", function () {
                return $.ajax('/top-refresh', {
                    data: { tab: Status.options.Tab },
                }).done(function (html) {
                    var tabs = $(html).filter('.top-tabs');
                    if (tabs.length) {
                        $('.top-tabs').replaceWith(tabs);
                    }
                    var issuesList = $(html).filter('.issues-list');
                    var curList = $('.issues-list');
                    if (issuesList.length) {
                        var issueCount = issuesList.data('issue-count');
                        // TODO: don't if hovering
                        if (issueCount > 0) {
                            if (!curList.children().length) {
                                curList.replaceWith(issuesList.find('.issues-button').fadeIn().end());
                            } else {
                                $('.issues-button').html($('.issues-button', issuesList).html());
                                $('.issues-dropdown').html($('.issues-dropdown', issuesList).html());
                            }
                        } else {
                            curList.fadeOut(function () {
                                $(this).empty();
                            });
                        }
                    }
                }).fail(Status.UI.ajaxError);
            }, Status.options.HeaderRefresh * 1000);
        }
        
        var resizeTimer;
        $(window).resize(function () {
            clearTimeout(resizeTimer);
            resizeTimer = setTimeout(function () {
                $(this).trigger('resized');
            }, 100);
        });
        $(document).on('click', '.reload-link', function (e) {
            var data = {
                type: $(this).data('type'),
                uniqueKey: $(this).data('uk'),
                guid: $(this).data('guid')
            };
            if (!data.type && (this.href || '#') != '#') return;
            if (data.type && data.uniqueKey) {
                // Node to refresh, do it
                var link = $(this).addClass('reloading');
                link.find('.js-text')
                    .text('Polling...');
                Status.refresh.pause();
                $.post('/poll', data)
                    .fail(function () {
                        toastr.error('There was an error polling this node.', 'Polling',
                        {
                            positionClass: 'toast-top-right-spaced',
                            timeOut: 5 * 1000
                        });
                    }).done(function () {
                        // TODO: Find nearest refresh parent after compartmentalization and refresh that node
                        window.location.reload(true);
                        //Status.refresh.resume();
                        //link.removeClass('reloading')
                        //    .find('.js-text')
                        //    .text('Poll Now');
                    });
            } else {
                window.location.reload(true);
            }
            e.preventDefault();
        }).on('click', '.issues-button', function (e) {
            $(this).parent('.issues-list').toggleClass('active');
            e.preventDefault();
        }).on('click', '.issues-list, .action-popup', function (e) {
            e.stopPropagation();
        }).on({
            'click': function () {
                $('.issues-list').removeClass('active');
                $('.action-popup').remove();
            },
            'show': function () {
                resumeRefresh();
            },
            'hide': function () {
                pauseRefresh();
            }
        });
        prepTableSorter();
    }

    return {
        init: init,
        getPopup: getPopup,
        showSummaryPopup: showSummaryPopup,
        summaryPopup: summaryPopup,
        resizePopup: resizePopup,
        ajaxLoaders: ajaxLoaders,
        refresh: {
            register: registerRefresh,
            pause: pauseRefresh,
            resume: resumeRefresh,
            run: runRefresh,
            registered: registeredRefreshes
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
        Status.Dashboard.options.filter = filter = (filter || '').toLowerCase();
        if (!filter) {
            $('.server-row[data-info], .node-group, .node-header').removeClass('hidden');
            //history.pushState({ filter: filter }, 'Dashboard Search: ' + filter, '/dashboard');
            return;
        } else {
            //history.pushState({ filter: filter }, 'Dashboard Search: ' + filter, '/dashboard?filter=' + encodeURIComponent(filter));
        }
        $('.server-row[data-info]').each(function () {
            var show = $(this).data('info').indexOf(filter) > -1;
            $(this).toggleClass('hidden', !show); //[show ? 'show' : 'hide']()
        });
        $('.node-group').each(function () {
            var show = $('.server-row:not(.hidden)', this).length;
            $(this).prev('.node-header').toggleClass('hidden', !show);
        });
    }

    function init(options) {
        Status.Dashboard.options = options;

        if (options.refresh) {
            Status.refresh.register("Dashboard", function () {
                return $.ajax(Status.Dashboard.options.refreshUrl || window.location.href, {
                    data: $.extend({}, Status.Dashboard.options.refreshData),
                    cache: false
                }).done(function (html) {
                    var serverGroups = $('.node-group[data-name], .node-header[data-name], .refresh-group[data-name]', html);
                    $('.node-group[data-name], .node-header[data-name], .refresh-group[data-name]').each(function () {
                        var name = $(this).data('name'),
                            match = serverGroups.filter('[data-name="' + name + '"]');
                        if (!match.length) {
                            console.log('Unable to find: ' + name + '.');
                        } else {
                            $(this).replaceWith(match);
                        }
                    });
                    $('.cluster-sub-detail').replaceWith($(html).filter('.cluster-sub-detail'));
                    if (Status.Dashboard.options.filter)
                        applyFilter(Status.Dashboard.options.filter);
                    if (Status.Dashboard.options.afterRefresh)
                        Status.Dashboard.options.afterRefresh();
                }).fail(function () { console.log(this, arguments); });
            }, Status.Dashboard.options.refresh * 1000);
        }

        $('.node-dashboard').on('click', '.dashboard-spark', function() {
            var $this = $(this),
                id = $this.data('id'),
                title = $this.data('title'),
                max = $this.closest('[data-max]').data('max'),
                type = title.toLowerCase(),
                row = $this.closest('tr'),
                node = row.data('node') || row.find('.node-name-link').text(),
                subtitle = $this.parent().data('subtitle') || node;

            switch (type) {
            case 'cpu':
                $('#dashboard-chart').empty().cpuGraph({ id: id, title: 'CPU Utilization', subtitle: subtitle, animate: true });
                break;
            case 'memory':
                $('#dashboard-chart').empty().memoryGraph({ id: id, title: 'Memory Utilization', subtitle: subtitle, animate: true, max: max });
                break;
            default:
                return;
            }

            $('#spark-detail').modal({
                overlayClose: true,
                onClose: function(dialog) {
                    dialog.data.fadeOut('fast', function() {
                        dialog.container.hide('fast', function() {
                            $.modal.close();
                        });
                    });
                }
            });
        });

        $(document).on('keyup', '.node-ddl, .top-filter input', function () {
            applyFilter(this.value);
        });
        if (Status.Dashboard.options.filter)
            $('.node-category-list .filter-box input').keyup();
    }

    return {
        init: init
    };

})();

Status.Dashboard.Server = (function () {

    var currentRequest;

    function init(options) {
        Status.Dashboard.Server.options = options;

        $('.sql-server').on('click', '.sortable a', function() {
            var query = $(this).attr('href'),
                fullUrl = '/sql/top' + query;

            $(this).closest('th').addClass('loading').siblings().removeClass('loading');
            if (currentRequest) currentRequest.abort();
            currentRequest = $.get(fullUrl, function(html) {
                var newTable = $('.sql-server .node-dashboard', html);
                if (newTable.length) {
                    $('.sql-server .node-dashboard').replaceWith(newTable);
                }
            });
            return false;
        }).on('submit', '.category-row form', function () {
            $(this).appendLoading();
            
            if (currentRequest) currentRequest.abort();
            currentRequest = $.get('/sql/top', $(this).serialize(), function (html) {
                var newTable = $('.sql-server .node-dashboard', html);
                if (newTable.length) {
                    $('.sql-server .node-dashboard').replaceWith(newTable);
                }
            });
            return false;
        });
        $('.realtime-cpu').on('click', function () {
            var start = +$(this).parent().siblings('.total-cpu-percent').text().replace(' %','');
            liveDashboard(start);
            return false;
        });
        setupInterfaces();
    }
    
    function setupInterfaces() {
        var titleRow = $('table.interface-dashboard > thead tr.category-row').remove();
        
        $('table.interface-dashboard').tablesorter({
            headers: {
                0: { sorter: false },
                1: { sorter: 'dataVal' },
                2: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                3: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                4: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                5: { sorter: 'dataVal', sortInitialOrder: 'desc' },
                6: { sorter: 'dataVal', sortInitialOrder: 'desc' }
            }
        });
        titleRow.prependTo($('table.interface-dashboard > thead '));
    }
    
    function liveDashboard(startValue) {

        if ($('#cpu-graph').length) return;

        var container = '<div id="cpu-graph"><div class="cpu-total"><div id="cpu-total-graph" class="chart" data-title="Total CPU Utilization"></div></div></div>',
            wrap = $(container).appendTo('.bottom-section'),
            liveGraph = $('#cpu-total-graph').lived3graph({
                type: 'cpu',
                width: 'auto',
                height: 250,
                startValue: startValue,
                params: { node: Status.Dashboard.Server.options.nodeName },
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

        $.extend(Status.ajaxLoaders, {
            '#/elastic/summary/': function (val) {
                Status.Dashboard.options.refreshData = { popup: val };
                Status.summaryPopup('/elastic/node/summary/' + val, options, false, function() {
                    Status.Dashboard.options.refreshData = {};
                });
            },
            '#/elastic/index/': function (val) {
                var parts = val.split('/');
                if (parts.length != 2) {
                    console.log('Unrecognized index string: ' + val);
                    return;
                }
                var reqOptions = $.extend({}, options, { index: parts[0] });
                Status.Dashboard.options.refreshData = {
                    index: parts[0],
                    popup: parts[1]
                };
                Status.summaryPopup('/elastic/index/summary/' + parts[1], reqOptions, false, function () {
                    Status.Dashboard.options.refreshData = {};
                });
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

        $('.node-ddl').on('click', function () {
            var val = $(this).val();
            $(this).select().trigger('focus').removeClass('icon').val('');
            setTimeout(function () {
                var selected = $('.status-icon[data-host="' + val + '"]');
                if (selected.length) {
                    var top = selected.closest('li').addClass('ac_over').position().top;
                    $('.ac_results ul').scrollTop(top);
                }
            }, 0);
        }).autocomplete(options.nodes, {
            max: 500,
            minChars: 0,
            matchContains: true,
            delay: 50,
            formatItem: function (row) {
                return '<span class="status-icon ' + row.sClass + ' icon" data-host="' + row.node + '">●</span> <span class="server-category-label">' + row.category + ':</span> <span class="' + row.sClass + '">' + row.name + '</span>';
            },
            formatResult: function (row) { return row.node; },
            formatMatch: function (row) { return row.category + ': ' + row.node; }
        }).result(function (e, data) {
            $(this).addClass('left-icon ' + data.sClass).closest('form').submit();
        }).keydown(function (e) {
            return e.keyCode != 13;
        });
        $('.server-search').on('click', '.js-show-all-down', function () {
            $(this).siblings('.hidden').removeClass('hidden').end().remove();
        });
        $('.node-category-list').on('click', '.filters-current', function (e) {
            if (e.target.tagName != 'INPUT') $('.filters, .filters-toggle').toggle();
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
        var wrap = Status.getPopup();
        wrap.load('/sql/servers', $.extend({}, Status.Dashboard.options.refreshData, {
            detailOnly: true
        }), function () {
            Status.showSummaryPopup(function () {
                Status.Dashboard.options.refreshData = {};
            }, 50);
        });
    }

    function loadPlan(val) {
        var parts = val.split('/'),
            handle = parts[0],
            offset = parts.length > 1 ? parts[1] : null;
        $('.sql-server .plan-row[data-plan-handle="' + handle + '"]').addClass('selected');
        var wrap = Status.getPopup();
        wrap.load('/sql/top/detail', { node: Status.SQL.options.node, handle: handle, offset: offset }, function() {
            $('.query-col').removeClass('loading');
            Status.showSummaryPopup(function () { $('.plan-row.selected').removeClass('selected'); });
            prettyPrint();
            wrap.find('.sql-query-section .tabs-links').on('click', 'a', function () {
                if ($(this).hasClass('selected')) return false;
                $(this).addClass('selected').siblings().removeClass('selected');
                var newDiv = $(this).data('div');
                $('.sql-query-section .' + newDiv).show().siblings().not('.tabs').hide();

                Status.resizePopup();

                $('.qp-root').drawQueryPlanLines();
                return false;
            });
            var currentTt;
            wrap.find('.qp-node').hover(function() {
                var pos = $(this).offset();
                var tt = $(this).find('.qp-tt');
                currentTt = tt.clone();
                currentTt.addClass('sql-query-tooltip')
                    .appendTo(document.body)
                    .css({ top: pos.top + $(this).outerHeight(), left: pos.left })
                    .show();
            }, function() {
                if (currentTt) currentTt.hide();
            });
            wrap.find('.handle-link a').on('click', function() {
                if (!confirm('Are you sure you want to remove this plan from cache?'))
                    return false;

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
            wrap.find('.show-toggle').on('click', function (e) {
                var grad = $(this).closest('.hide-gradient'),
                    excerpt = grad.prev('.sql-query-excerpt');
                excerpt.animate({ 'max-height': excerpt[0].scrollHeight }, 400, function () { $(window).resize(); });
                grad.fadeOut(400);
                e.preventDefault();
            });
        });
    }
    
    function init(options) {
        Status.SQL.options = options;
        
        $.extend(Status.ajaxLoaders, {
            '#/cluster/': loadCluster,
            '#/plan/': loadPlan,
            '#/sql/summary/': function (val) {
                Status.summaryPopup('/sql/instance/summary/' + val, { node: Status.SQL.options.node });
            },
            '#/db/': function(val) {
                Status.summaryPopup('/sql/db/' + val, { node: Status.SQL.options.node }, true);
            }
        });        
        
        $('.sql-server').on('click', '.plan-row', function() {
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
            if (e.keyCode == 13) {
                $(this).submit();
                $('.filters').hide();
                $('.filters-current').addClass('loading');
            }
        }).on('click', '.filters, .filters-current', function (e) {
            e.stopPropagation();
        });
        $(document).on('click', function () {
            $('.filters').toggle(false);
        }).on('click', '.sql-toggle-agent-job', function () {
            var $link = $(this).addClass('loading');
            $.ajax('/sql/toggle-agent-job', {
                type: 'POST',
                data: {
                    node: Status.SQL.options.node,
                    guid: $(this).data('guid'),
                    enable: $(this).data('enable')
                },
                success: function (data, status, xhr) {
                    if (data === true) {
                        Status.summaryPopup('/sql/instance/summary/jobs', { node: Status.SQL.options.node }, true);
                        Status.resizePopup();
                    } else {
                        $link.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred toggling this job.');
                    }
                },
                error: function (xhr) {
                    $link.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred toggling this job.');
                }
            });
            return false;
        });
        $('#content').on('click', '.ag-node', function() {
            window.location.hash = $('.ag-node-name a', this)[0].hash;
        });
    }

    return {
        init: init
    };

})();

Status.Redis = (function () {
    
    function init(options) {
        Status.Redis.options = options;

        $.extend(Status.ajaxLoaders, {
            '#/redis/summary/': function(val) {
                Status.summaryPopup('/redis/instance/summary/' + val, { node: Status.Redis.options.node + ':' + Status.Redis.options.port });
            }
        });

        $('#content').on('click', '.js-redis-role-action', function (e) {
            var link = this;
            Status.refresh.pause("Dashboard");
            $.get('redis/instance/actions/role', { node: $(this).data('node') }, function (data) {
                $(link).parent().actionPopup(data);
            });
            e.preventDefault();
        });
        
        $('<div class="expand">show all</div>').click(function () {
            $(this).prev().removeClass('collapsed').end().remove();
        }).insertAfter($('.collapsed .info-line:nth-child(4)').parent());
    }

    return {
        init: init
    };

})();

Status.Exceptions = (function () {

    function refreshCounts(data) {
        if (!data.length) return;
        var apps = {}, total = 0;
        for (var i = 0; i < data.length; i++) {
            apps[data[i].Name] = data[i];
            total += data[i].ExceptionCount;
        }
        $('.top-server-list li a span').each(function () {
            var app = apps[$(this).text()];
            $(this)
                .parent()
                .attr('title', app ? (app.ExceptionCount + ' Exception' + (app.ExceptionCount == 1 ? '' : 's') + (app.MostRecent ? ', Last: ' + app.MostRecent : '')) : '0 Exceptions')
                .siblings('span.count')
                .text(app && app.ExceptionCount ? ' (' + app.ExceptionCount.toLocaleString() + ')' : '');
        });
        if (Status.Exceptions.options.search) return;
        var log = Status.Exceptions.options.log;
        if (log) {
            var count = apps[log].ExceptionCount;
            $('.exception-title').text(count.toLocaleString() + ' ' + log + ' Exception' + (count != 1 ? 's' : ''));
        } else {
            $('.exception-title').text(total.toLocaleString() + ' Exception' + (total != 1 ? 's' : ''));
        }
        $('.tabs-links .count.exception-count').text(total);
    }

    function init(options) {
        Status.Exceptions.options = options;

        function getLoadCount() {
            // If scrolled back to the top, load 500
            if ($(window).scrollTop() == 0) {
                return 500;
            }
            return Math.max($('.exceptions-dashboard tbody tr').length, 500);
        }

        if (options.refresh) {
            Status.refresh.register("Status.Exceptions", function () {
                return $.ajax(window.location.href, {
                    data: { sort: options.sort, count: getLoadCount() },
                    cache: false
                }).done(function (html) {
                    var newPage = $(html),
                        newHeader = $('.top-server-list', newPage),
                        newRows = $('.exceptions-dashboard > tbody > tr', newPage),
                        newDB = $('.exceptions-dashboard', newPage),
                        newCount = newDB.data('total-count'),
                        newTitle = newDB.data('title');
                    $('.exception-count').text((+newCount).toLocaleString());
                    $('.exception-title').text(newTitle);
                    if (newTitle) document.title = Status.options.SiteName ? newTitle + ' - ' + Status.options.SiteName : newTitle;
                    $('.top-server-list').replaceWith(newHeader);
                    $('.exceptions-dashboard tbody').empty().append(newRows);
                }).fail(Status.UI.ajaxError);
            }, Status.Exceptions.options.refresh * 1000);
        }

        var loadingMore = false,
            allDone = false;

        function loadMore() {
            if (loadingMore || allDone) return;

            if ($(window).scrollTop() + $(window).height() > $(document).height() - 100) {
                // TODO: loading indicator
                loadingMore = true;
                $('.loading-more-spacer').show();
                var lastGuid = $('.exceptions-dashboard tr.error').last().data('id');
                $.ajax('/exceptions/load-more', {
                    data: { log: options.log, sort: options.sort, count: options.loadMore, prevLast: lastGuid },
                    cache: false
                }).done(function (html) {
                    var newRows = $(html).filter('tr');
                    $('.exceptions-dashboard tbody').append(newRows);
                    $('.loading-more-spacer').hide();
                    if (newRows.length < options.loadMore) {
                        allDone = true;
                        $('.no-more').fadeIn();
                    }
                    loadingMore = false;
                });
            }
        }

        if (options.loadMore) {
            allDone = $('.exceptions-dashboard tbody tr.error').length < 250;
            $(document).on('scroll exception-deleted', loadMore);
        }


        // ajax the error deletion on the list page
        $('.bottom-section').on('click', '.exceptions-dashboard a.delete-link', function () {
            var jThis = $(this);

            // if we're already deleted, bomb out early
            if (jThis.closest('.error.deleted').length) return false;

            // if we've "protected" this error, confirm the deletion
            if (jThis.closest('tr.protected').length && !confirm('Really delete this protected error?')) return false;

            var url = jThis.attr('href'),
                jRow = jThis.closest('tr'),
                jCell = jThis.closest('td').addClass('loading');

            $.ajax({
                type: 'POST',
                data: { log: jRow.data('log') || options.log, id: jRow.data('id') },
                context: this,
                url: url,
                success: function (data) {
                    if (options.showingDeleted) {
                        jThis.attr('title', 'Error is already deleted');
                        jCell.removeClass('loading');
                        jRow.addClass('deleted');
                        jCell.find('span.protected').replaceWith('<a title="Undelete and protect this error" class="protect-link" href="' + url.replace('/delete', '/protect') + '">&nbsp;P&nbsp;</a>');
                    } else {
                        var table = jRow.closest('table');
                        if (!jRow.siblings().length) {
                            $('.clear-all-div').remove();
                            $('.no-content').fadeIn('fast');
                        }
                        jRow.closest('tr').remove();
                        table.trigger('update', [true]);
                        refreshCounts(data);
                        $(document).trigger('exception-deleted');
                    }
                },
                error: function (xhr) {
                    $(this).attr('href', url);
                    jCell.removeClass('loading').errorPopupFromJSON(xhr, 'An error occurred deleting');
                }
            });
            return false;
        });

        // ajax the protection on the list page
        $('.bottom-section').on('click', '.exceptions-dashboard a.protect-link', function () {
            var url = $(this).attr('href'),
                jRow = $(this).closest('tr'),
                jCell = $(this).closest('td').addClass('loading');

            $.ajax({
                type: 'POST',
                data: { log: jRow.data('log') || options.log, id: jRow.data('id') },
                context: this,
                url: url,
                success: function (data) {
                    $(this).siblings('.delete-link').attr('title', 'Delete this error')
                           .end().replaceWith('<span title="This error is protected" class="protected"></span>');
                    jRow.addClass('protected').removeClass('deleted');
                    refreshCounts(data);
                },
                error: function (xhr) {
                    $(this).attr('href', url);
                    jCell.errorPopupFromJSON(xhr, 'An error occurred protecting');
                },
                complete: function () {
                    jCell.removeClass('loading');
                }
            });
            return false;
        });
        
        var lastSelected;

        if (options.log) {
            $('.bottom-section').on('click', '.exceptions-dashboard tbody td:nth-child(2), .exceptions-dashboard tbody td:nth-child(3)', function (e) {
                var row = $(this).closest('tr');
                row.toggleClass('selected');

                if (e.shiftKey) {
                    var index = row.index(),
                        lastIndex = lastSelected.index();
                    if (!e.ctrlKey) {
                        row.siblings().andSelf().removeClass('selected');
                    }
                    row.parent()
                       .children()
                       .slice(Math.min(index, lastIndex), Math.max(index, lastIndex)).add(lastSelected).add(row)
                       .addClass('selected');
                    if (!e.ctrlKey) {
                        lastSelected = row.first();
                    }
                } else if (e.ctrlKey) {
                    lastSelected = row.first();
                } else {
                    if ($('.exceptions-dashboard tbody td').length > 2) {
                        row.addClass('selected');
                    }
                    row.siblings().removeClass('selected');
                    lastSelected = row.first();
                }
            });
            $(document).keydown(function(e) {
                if (e.keyCode == 46 || e.keyCode == 8) {
                    var selected = $('.error.selected').not('.protected');
                    if (selected.length > 0) {
                        var ids = selected.map(function () { return $(this).data('id'); }).get();
                        selected.children('.actions').addClass('loading');

                        $.ajax({
                            type: 'POST',
                            context: this,
                            traditional: true,
                            data: { log: options.log, ids: ids, returnCounts: true },
                            url: '/exceptions/delete-list',
                            success: function (data) {
                                var table = selected.closest('table');
                                selected.remove();
                                table.trigger('update', [true]);
                                refreshCounts(data);
                            },
                            error: function (xhr) {
                                selected.children('.actions').removeClass('loading');
                                selected.last().children().first().errorPopupFromJSON(xhr, 'An error occurred clearing selected exceptions');
                            }
                        });
                        return false;
                    }
                }
            });
        }

        $('#content').on('click', 'a.clear-all-link', function () {
            if (confirm('Really delete all non-protected errors?')) {
                $(this).addClass('loading');

                $.ajax({
                    type: 'POST',
                    context: this,
                    data: { log: $(this).data('log') || options.log, id: $(this).data('id') || options.id },
                    url: $(this).attr('href'),
                    success: function (data) {
                        window.location.href = data.url;
                    },
                    error: function (xhr) {
                        $(this).removeClass('loading');
                        $(this).parent().errorPopupFromJSON(xhr, 'An error occurred clearing this log');
                    }
                });
            }
            return false;
        });

        $('a.clear-visible-link').on('click', function () {
            if (confirm('Really delete all visible non-protected errors?')) {
                var ids = $('.exceptions-dashboard tr.error:not(.protected,.deleted)').map(function () { return $(this).data('id'); }).get();
                $(this).addClass('loading');

                $.ajax({
                    type: 'POST',
                    context: this,
                    traditional: true,
                    data: { log: options.log, ids: ids },
                    url: '/exceptions/delete-list',
                    success: function (data) {
                        window.location.href = data.url;
                    },
                    error: function (xhr) {
                        $(this).removeClass('loading');
                        $(this).parent().errorPopupFromJSON(xhr, 'An error occurred clearing visible exceptions');
                    }
                });
            }
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
            var previewTimer = 0;
            $('.bottom-section').on({
                mouseenter: function () {
                    var jThis = $(this).find('a.exception-link'),
                        url = jThis.attr('href').replace('/detail', '/preview');

                    clearTimeout(previewTimer);
                    previewTimer = setTimeout(function () {
                        $.get(url, function (resp) {
                            var sane = $(resp).filter('.error-preview');
                            if (!sane.length) return;

                            $('.error-preview-popup').fadeOut(125, function () { $(this).remove(); });
                            var errDiv = $('<div class="error-preview-popup" />').append(resp);
                            errDiv.appendTo(jThis.parent()).fadeIn('fast');
                        });
                    }, 800);
                },
                mouseleave: function () {
                    clearTimeout(previewTimer);
                    $('.error-preview-popup', this).fadeOut(125, function () { $(this).remove(); });
                }
            }, '.exceptions-dashboard .exception-cell');
        }

        /* Error detail handlers*/
        $('.info-delete-link a').on('click', function () {
            $(this).addClass('loading');
            $.ajax({
                type: 'POST',
                data: { id: options.id, log: options.log, redirect: true },
                context: this,
                url: '/exceptions/delete',
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
                data: { id: options.id, log: options.log, actionid: actionid },
                context: this,
                url: '/exceptions/jiraaction',
                success: function (data) {
                    $(this).removeClass('loading');
                    if (data.success == true) {
                        if (data.browseUrl != null && data.browseUrl != "") {

                            var issueLink = '<a href="' + data.browseUrl + '" target="_blank">' + data.issueKey + '</a>';
                            $("#jira-links-container").show();
                            $("#jira-links-container").append('<span> ( ' + issueLink + ' ) </span>')
                            toastr.success('<div style="margin-top:5px">' + issueLink + '</div>', 'Issue Created')
                        }
                        else {
                            toastr.success("Issue created : " + data.issueKey, 'Success')
                        }

                    }
                    else {
                        toastr.error(data.message, 'Error')
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

Status.Graphs = (function () {
    function init(options) {
        Status.Graphs.options = options;

        $(function () {
            $('.sub-tabs').on('click', 'a', function (e) {
                var range = $(this).data('range');
                if (e.ctrlKey || e.shiftKey || range == 'now' || !range) { return true; }
                $(this).addClass('selected').siblings().removeClass('selected');
                Status.Graphs.selectRange(range);
                return false;
            });
        });
    }

    function selectRange(range) {
        Status.Graphs.options.selectedRange = range;
        var ranges = Status.Graphs.options.ranges;
        for (var i = 0; i < ranges.length; i++) {
            if (ranges[i].text == range) {
                Status.Graphs.options.start = ranges[i].start;
            }
        }

        $('rect ~ text tspan').filter(function () {
            return $(this).text() == range;
        }).trigger('click');
    }

    return {
        init: init,
        selectRange: selectRange,
        count: 0
    };
})();

Status.HAProxy = (function () {
    var refreshLink;

    function init(options) {
        Status.HAProxy.options = options;

        if (options.refresh) {
            Status.refresh.register("Status.HAProxy", function () {
                return $.ajax(window.location.pathname, {
                    data: { group: Status.HAProxy.options.group, watch: Status.HAProxy.options.proxy },
                }).done(function (html) {
                    var proxies = $('.proxies-wrap, .dashboard-wrap', html);
                    $('.proxies-wrap, .dashboard-wrap').replaceWith(proxies);
                    refreshLink.detach();
                    var header = $('.node-category-list.top-section', html);
                    $('.node-category-list.top-section').replaceWith(header);
                    $('.refresh-link').replaceWith(refreshLink);
                }).fail(function (xhr) {
                    toastr.error((xhr.responseText ? 'There was an error loading: ' + xhr.responseText : 'Unknwon error refreshing') + ' at ' + new Date(),
                        "Problem refreshing",
                        {
                            positionClass: "toast-bottom-full-width",
                            timeOut: (Status.HAProxy.options.refresh || 5) * 1000 - 1000
                        });
                });
            }, (Status.HAProxy.options.refresh || 5) * 1000);
        }

        refreshLink = $('.refresh-link');
        
        function stopRefresh() {
            Status.refresh.pause();
            refreshLink.text('Enable Refresh');
        }
        function startRefresh() {
            Status.refresh.resume();
            refreshLink.text('Disable Refresh');
        }

        refreshLink.on('click', function () {
            if ($(this).text() == 'Disable Refresh') {
                stopRefresh();
            } else {
                startRefresh();
            }
            return false;
        });
        
        // Admin Panel (security is server-side)
        // Proxy level clicks
        $('#content').on('click', '.haproxy-dashboard a.action-icon', function (e) {
            var $this = $(this),
                group = $this.data('group'),
                proxy = $this.data('proxy'),
                action = $this.data('action'),
                server = $this.data('server');
            
            if ($this.hasClass('disabled')) return false;
            
            // if we're at the proxy level, prompt for confirmation
            if (!server && !e.ctrlKey && !confirm('Are you sure you wish to ' + action.toLowerCase() + ' all of ' + proxy + '?'))
                return false;
            
            $this.addClass('loading');
            $.ajax({
                type: 'POST',
                data: { group: group, proxy: proxy, server: server, act: action }, //act: oh MVC
                context: this,
                url: '/haproxy/admin/proxy',
                success: function (data) {
                    if (data === true) {
                        startRefresh();
                    } else {
                        stopRefresh();
                    }
                },
                error: function () {
                    $(this).removeClass('loading').parent().errorPopup('An error occured while trying to ' + action + '.');
                }
            });
            return false;
        });

        $('#content').on('click', '.haproxy-dashboard-servers a.action-icon', function () {
            var $this = $(this),
                action = $this.data('action'),
                group = $this.data('group'),
                server = $this.data('server');
            
            if ($this.hasClass('disabled')) return false;

            if (group && !confirm('Are you sure you with to ' + action.toLowerCase() + ' every server in ' + group + '?'))
                return false;

            $this.addClass('loading');
            $.ajax({
                type: 'POST',
                data: { server: server, act: action, group: group }, //act: oh MVC
                context: this,
                url: '/haproxy/admin/' + (server ? 'server' : 'group'),
                success: function (data) {
                    if (data === true) {
                        startRefresh();
                    } else {
                        stopRefresh();
                    }
                },
                error: function () {
                    $(this).removeClass('loading').parent().errorPopup('An error occured while trying to ' + action + '.');
                }
            });
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
        if (bytes == 0) return '0 ' + (zeroLabel || sizes[0]);
        var ubytes = Math.abs(bytes);
        var i = parseInt(Math.floor(Math.log(ubytes) / Math.log(1024)));
        var dec = (ubytes / Math.pow(1024, i)).toFixed(1).replace('.0', '');
        return dec + ' ' + sizes[i];
    }
    
    function commify(num) {
        return (num + '').replace(/(\d)(?=(\d\d\d)+(?!\d))/g, "$1,");
    }
    
    function getExtremes(array, series, min, max, stacked) {
        if (max == 'auto' || min == 'auto') {
            var maximums = { up: 0, down: 0 };
            if (stacked) {
                min = 0;
                max = d3.max(array, function (p) {
                    return d3.sum(series, function (s) { return p[s.name]; });
                    //var pointMax = 0;
                    //series.forEach(function (s) { pointMax += p[s.name] || 0; });
                    //return pointMax;
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

    // Creating jQuery plguins...
    $.fn.extend({
        loadingSpinner: function (wrap) {
            this.html('<div class="graph-loading">Loading...<br /><img src="/Content/img/ajax-loader.gif" alt="Loading graph..." /></div>');
            if (wrap) {
                this.wrap(wrap);
            }
            return this;
        },
        appendSpinner: function () {
            return this.append('<div class="graph-loading">Loading...<br /><img src="/Content/img/ajax-loader.gif" alt="Loading graph..." /></div>');
        },
        removeSpinner: function () {
            this.find('.graph-loading').remove();
            return this;
        },
        appendLoading: function () {
            return this.append('<img src="/Content/img/loading.gif" alt="Loading..." />');
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
            return this.addClass('chart').d3graph(
                $.extend(true, {}, {
                    type: 'cpu',
                    series: [{ name: 'value', label: 'CPU' }],
                    yAxis: {
                        tickLines: true,
                        tickFormat: function (d) { return d + '%'; }
                    },
                    showBuilds: true,
                    width: 'auto',
                    height: 'auto',
                    max: 100,
                    leftMargin: 40,
                    areaTooltipFormat: function(value) { return '<span class="label">CPU: </span><b>' + value + '%</b>'; }
                }, options));
        },
        memoryGraph: function (options) {
            return this.addClass('chart').d3graph(
                $.extend(true, {}, {
                    type: 'memory',
                    series: [{ name: 'value', label: 'Memory' }],
                    yAxis: {
                        tickLines: true,
                        tickFormat: function (d) { return Status.helpers.bytesToSize(d * 1024 * 1024, true, 'GB'); }
                    },
                    showBuilds: true,
                    width: 'auto',
                    height: 'auto',
                    leftMargin: 60,
                    max: this.data('max'),
                    areaTooltipFormat: function (value) { return '<span class="label">Memory: </span><b>' + Status.helpers.bytesToSize(value * 1024 * 1024, true) + '</b>'; },
                }, options));
        },
        networkGraph: function (options) {
            return this.addClass('chart').d3graph({
                type: 'network',
                series: [
                    { name: 'main_in', label: 'In' },
                    { name: 'main_out', label: 'Out', direction: 'down' }
                ],
                width: 'auto',
                min: 'auto',
                leftMargin: 60,
                areaTooltipFormat: function (value, series, name) { return '<span class="label">Bandwidth (<span class="series-' + name + '">' + series + '</span>): </span><b>' + Status.helpers.bytesToSize(value, false) + '/s</b>'; },
                yAxis: {
                    tickFormat: function (d) { return Status.helpers.bytesToSize(d, false); }
                }
            }, options);
        },   
        haproxyGraph: function (options) {
            var comma = d3.format(',');
            return this.addClass('chart').d3graph({
                type: 'haproxy',
                subtype: 'traffic',
                noAjaxZoom: true,
                series: [
                    { name: 'main_hits', label: 'Total' },
                    { name: 'main_pages', label: 'Pages' }
                ],
                width: 'auto',
                height: 380,
                min: 'auto',
                leftMargin: 80,
                areaTooltipFormat: function (value, series, name) { return '<span class="label">Hits (<span class="series-' + name + '">' + series + '</span>): </span><b>' + comma(value) + '</b>'; },
                yAxis: {
                    tickFormat: comma // function (d) { return comma(d); }
                }
            }, options);
        },
        haproxyRouteGraph: function (route, days, host, options) {
            return this.d3graph({
                type: 'haproxy',
                subtype: 'route-performance',
                noAjaxZoom: true,
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
                width: 1000,
                min: 'auto',
                leftMargin: 60,
                rightAreaTooltipFormat: function (value, series, name, color) {
                    return '<span class="label">' + (color ? '<div style="background-color: ' + color + '; width: 16px; height: 13px; display: inline-block;"></div> ' : '')
                        + '<span class="series-' + name + '">' + series + '</span>: </span><b>' + Status.helpers.commify(value) + '</b>';
                },
                areaTooltipFormat: function (value, series, name, color) {
                    return '<span class="label">' + (color ? '<div style="background-color: ' + color + '; width: 16px; height: 13px; display: inline-block;"></div> ' : '')
                        + '<span class="series-' + name + '">' + series + '</span>: </span><b>' + Status.helpers.commify(value) + ' <span class="note">ms</span></b>';
                },
                yAxis: {
                    tickFormat: function (d) { return Status.helpers.commify(d) + ' ms'; }
                }
            }, options);
        },
        
        d3graph: function (options, addOptions) {
            var defaults = {
                series: [{ name: 'value', label: 'Main' }],
                showBuilds: false,
                noAjaxZoom: false,
                stacked: false,
                autoColors: false,
                dateRanges: true,
                interpolation: 'linear',
                leftMargin: 40,
                id: this.data('id'),
                title: this.data('title'),
                subtitle: this.data('subtitle'),
                start: this.data('start') || Status.Graphs.options && Status.Graphs.options.start,
                end: this.data('end') || Status.Graphs.options && Status.Graphs.options.end,
                width: 660,
                height: 300,
                max: 'auto',
                min: 0
            };
            if (Status.Graphs && Status.Graphs.options) {
                options = $.extend({}, Status.Graphs.options, options, addOptions);
            }
            options = $.extend({}, defaults, options);

            var minDate, maxDate,
                topBrushArea, bottomBrushArea,
                dataLoaded,
                curWidth, curHeight, curData,
                chart = this,
                endDate = options.end ? new Date(options.end) : new Date(),
                dateRanges = dateRanges ? {
                    'Day': new Date((endDate.getTime() - 24 * 60 * 60 * 1000)),
                    'Week': new Date((endDate.getTime() - 7 * 24 * 60 * 60 * 1000)),
                    'Month': new Date(new Date(endDate).setMonth(endDate.getMonth() - 1)),
                    '6 Months': new Date(new Date(endDate).setMonth(endDate.getMonth() - 6))
                } : {},
                rangeSelections = $('<div class="range-selection" />').appendTo(chart),
                buildTooltip = $('<div class="build-tooltip chart-tooltip" />').appendTo(chart),
                areaTooltip = $('<div class="area-tooltip chart-tooltip" />').appendTo(chart),
                series = options.series,
                rightSeries = options.rightSeries,
                leftPalette = options.autoColors === true ? options.leftPalette || 'PuBu' : (options.autoColors || null),
                rightPalette = options.autoColors === true ? options.rightPalette || 'Greens' : (options.rightPalette || options.autoColors || null),
                margin, margin2, width, height, height2,
                x, x2, y, yr, y2,
                xAxis, xAxis2, yAxis, yrAxis,
                brush, brush2,
                clipId = 'clip' + Status.Graphs.count++,
                gradientId = 'gradient-' + Status.Graphs.count,
                svg, focus, context, clip,
                currentArea,
                refreshTimer,
                urlPath = '/graph/' + options.type + (options.subtype ? '/' + options.subtype : '') + '/json',
                params = { summary: true },
                stack, stackArea, stackSummaryArea, stackFunc; // stacked specific vars
            
            if (options.id) params.id = options.id;
            if (options.start) params.start = options.start / 1000;
            if (options.end) params.end = options.end / 1000;
            $.extend(params, options.params);
            
            options.width = options.width == 'auto' ? (chart.width() - 10) : options.width;
            options.height = options.height == 'auto' ? (chart.height() - 40) : options.height;

            for (var range in dateRanges) {
                $('<button />', { data: { start: dateRanges[range] } }).text(range).appendTo(rangeSelections);
            }

            if (options.title) {
                var titleDiv = $('<div class="chart-title"/>').text(options.title).prependTo(chart);
                if (options.subtitle) {
                    $('<div class="chart-subtitle"/>').text(options.subtitle).appendTo(titleDiv);
                }
            }
            
            function drawElements() {
                if (options.width - 10 - options.leftMargin < 300)
                    options.width = 300 + 10 + options.leftMargin;
                
                margin = { top: 10, right: options.rightMargin || 10, bottom: 100, left: options.leftMargin };
                margin2 = { top: options.height - 77, right: 10, bottom: 20, left: options.leftMargin };
                width = options.width - margin.left - margin.right;
                height = options.height - margin.top - margin.bottom;
                height2 = options.height - margin2.top - margin2.bottom;

                x = d3.time.scale.utc().range([0, width]);
                x2 = d3.time.scale.utc().range([0, width]);
                y = d3.scale.linear().range([height, 0]);
                yr = d3.scale.linear().range([height, 0]);
                y2 = d3.scale.linear().range([height2, 0]);

                xAxis = d3.svg.axis().scale(x).orient('bottom');
                xAxis2 = d3.svg.axis().scale(x2).orient('bottom');
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

                brush2 = d3.svg.brush()
                    .x(x2)
                    .on('brush', redrawFromSummary);
                
                svg = d3.select(chart[0]).append('svg')
                    .attr('width', options.width)
                    .attr('height', options.height);

                clip = svg.append('defs').append('clipPath')
                    .attr('id', clipId)
                    .append('rect')
                    .attr('width', width)
                    .attr('height', height);

                focus = svg.append('g').attr('transform', 'translate(' + margin.left + ',' + margin.top + ')');
                context = svg.append('g').attr('transform', 'translate(' + margin2.left + ',' + margin2.top + ')');
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
                x.domain(options.noAjaxZoom ? [Status.Graphs.options.start || minDate, Status.Graphs.options.end || maxDate] : d3.extent(data.points.map(function (d) { return d.date; })));
                x2.domain(d3.extent(data.summary.map(function (d) { return d.date; })));
                y2.domain(getExtremes(data.summary, series, options.min, options.max));

                rescaleYAxis(data, true);
                
                if (options.stacked) {
                    stack = d3.layout.stack().values(function (d) { return d.values; });
                    stackArea = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function(d) { return x(d.date); })
                        .y0(function(d) { return y(d.y0); })
                        .y1(function (d) { return y(d.y0 + d.y); });
                    stackSummaryArea = d3.svg.area()
                        .x(function(d) { return x2(d.date); })
                        .y0(function(d) { return y2(d.y0); })
                        .y1(function (d) { return y2(d.y0 + d.y); });
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
                        // and the summary
                        context.append('path')
                            .datum(data.summary)
                            .attr('class', getClass('summary-area', s))
                            .attr('fill', getColor())
                            .attr('d', s.summaryArea.y0(y2(0)));
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

                curWidth = chart.width();
                curHeight = chart.height();
            }

            function drawBuilds(data) {
                if (!options.showBuilds || !data.builds) return;
                
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
                    var negative = s.direction == 'down';
                    s.area = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function (d) { return x(d.date); })
                        .y1(function (d) { return y(negative ? -d[s.name] : d[s.name]); });
                    s.summaryArea = d3.svg.area()
                        .interpolate(options.interpolation)
                        .x(function(d) { return x2(d.date); })
                        .y1(function(d) { return y2(negative ? -d[s.name] : d[s.name]); });
                });
                if (rightSeries) {
                    rightSeries.forEach(function (s) {
                        var negative = s.direction == 'down';
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
                    tooltip = '<div class="tooltip-date">' + chartFunctions.tooltipTimeFormat(date) + ' <span class="note">UTC</span></div>',
                    data = options.noAjaxZoom ? curData.summary : curData.points,
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
                        fakeVal = options.interpolation == 'linear'
                            ? d3.interpolate(dateBefore[s.name], dateAfter[s.name])(through)
                            : val,
                            cPos = (s.direction == 'down' ? -1 : 1) * fakeVal;
                        tooltip += (options.rightAreaTooltipFormat || areaTooltipFormat)(val, s.label, s.name, gc ? gc(s, i) : null) + '<br/>';
                        currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + yr(cPos) + ')');
                    });
                }
                
                series.forEach(function (s, i) {
                    var val = d[s.name] || 0, gc = getColor(),
                        fakeVal = options.interpolation == 'linear'
                            ? d3.interpolate(dateBefore[s.name], dateAfter[s.name])(through)
                            : val;
                    runningTotal += fakeVal;
                    var cPos = (s.direction == 'down' ? -1 : 1)
                        * (options.stacked ? runningTotal : fakeVal);

                    tooltipRows.push(options.areaTooltipFormat(val, s.label, s.name, gc ? gc(s, i) : null) + '<br/>');
                    currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + y(cPos) + ')');
                });

                if (options.stacked) {
                    tooltipRows.reverse();
                }
                tooltip += tooltipRows.join('');

                areaTooltip.html(tooltip)
                    .css({ left: pos[0] + 80, top: pos[1] + 60 });
                    //.css({ left: pos[0] - (areaTooltip.width() / 2), top: pos[1] - areaTooltip.height() - 20 });

                currentArea.attr('transform', 'translate(' + (pos[0]) + ', 0)');
            }

            function onWindowResized() {
                var newWidth = chart.width(),
                    newHeight = chart.height();
                if (curWidth != newWidth || curHeight != newHeight) {
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
            
            $.getJSON(urlPath, params, function (data) {
                postProcess(data);
                prepSeries();
                drawPrimaryGraphs(data);
                drawBuilds(data);
                dataLoaded = true;
                
                // set the initial summary brush to reflect what was loaded up top
                brush2.extent(x.domain())(bottomBrushArea);

                if (options.showBuilds && !data.builds) {
                    $.getJSON('/graph/builds/json', params, function (bData) {
                        postProcess(bData);
                        drawBuilds(bData);
                    });
                }
            });

            function postProcess(data) {
                function process(name) {
                    if (data[name]) {
                        data[name].forEach(function(d) {
                            d.date = new Date(d.date);
                        });
                        curData[name] = data[name];
                    }
                }

                if (options.noAjaxZoom && !data.points)
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
                if (!options.noAjaxZoom) {
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

                if (options.noAjaxZoom) {
                    curData.points = curData.summary.filter(function (p) {
                        var t = p.date.getTime() / 1000;
                        return start <= t && t <= end;
                    });
                    rescaleYAxis(curData, true);
                } else {
                    //refresh with high-res goodness
                    clearTimeout(refreshTimer);
                    refreshTimer = setTimeout(function () {
                        $.getJSON(urlPath, { id: options.id, start: start, end: end }, function (newData) {
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
                }

                // redraw
                if (options.stacked) {
                    focus.selectAll('.area').attr('d', function (d) { return stackArea(d.values); });
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
                .removeClass('cpu-chart memory-chart network-chart')
                .addClass(options.type + (options.subtype ? '-' + options.subtype : '') + '-chart')
                .on('click', 'button', function () {
                    var start = $(this).data('start'),
                        end = endDate.getTime(); // options.end;
                    //if (!start && $(this).hasClass('export')) {
                    //    $('#svg-export').remove();
                    //    var form = $('<form />', { id: 'svg-export', method: 'POST', action: '/export' });
                    //    $('<input />', { name: 'fileName', value: 'Test' }).appendTo(form);
                    //    $('<input />', { name: 'type', value: 'image/png' }).appendTo(form);
                    //    $('<input />', { name: 'width', value: '960' }).appendTo(form);
                    //    $('<input />', { name: 'svg', value: (new XMLSerializer).serializeToString($('svg', chart)[0]) }).appendTo(form);
                    //    form.appendTo(document.body).submit();
                    //    return;
                    //}
                    brush2.extent([new Date(Math.max(start.getTime(), minDate)), new Date(Math.min(end, maxDate))])(bottomBrushArea); //set the range and redraw
                    redrawMain(brush2.extent());
                });
        },
        lived3graph: function(options, addOptions) {
            var defaults = {
                series: [{ name: 'value', label: 'Main' }],
                leftMargin: 40,
                id: this.data('id'),
                start: this.data('start') || Status.Graphs.options.start,
                end: this.data('end') || Status.Graphs.options.end,
                title: this.data('title'),
                subtitle: this.data('subtitle'),
                slideDurationMs: 1000,
                durationSeconds: 5 * 60,
                width: 660,
                height: 300,
                max: 'auto',
                min: 0
            };
            if (Status.Graphs && Status.Graphs.options) {
                options = $.extend({}, Status.Graphs.options, options, addOptions);
            }
            options = $.extend({}, defaults, options);

            var curWidth, curHeight,
                now = new Date(),
                series = options.series,
                curData = d3.range(60 * 10).map(function(i) {
                    var result = { date: new Date(+now - (60 * 10 * 1000) + (i * 1000)) };
                    series.forEach(function (s) {
                        result[s.name] = i == 60 * 10 ? 0 : null;
                    });
                    return result;
                }),
                chart = this,
                areaTooltip = $('<div class="area-tooltip chart-tooltip" />').appendTo(chart),
                margin, width, height,
                x, y, xAxis, yAxis,
                svg, focus, clip,
                clipId = 'clip' + Status.Graphs.count++,
                currentArea,
                urlPath = '/dashboard/node/poll/' + options.type + (options.subtype ? '/' + options.subtype : ''),
                params = $.extend({}, { id: options.id, start: options.start / 1000, end: options.end / 1000 }, options.params);

            options.width = options.width == 'auto' ? (chart.width() - 10) : options.width;
            options.height = options.height == 'auto' ? (chart.height() - 40) : options.height;

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
                    var negative = s.direction == 'down';
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
                    tooltip = '<div class="tooltip-date">' + chartFunctions.tooltipTimeFormat(date) + ' <span class="note">UTC</span></div>';

                // align the moons! or at least the series hover dots
                series.forEach(function (s) {
                    var data = curData,
                        index = bisector(data, date, 1), // bisect the curData array to get the index of the hovered date
                        dateBefore = data[index - 1],    // get the date before the hover
                        dateAfter = data[index],         // and the date after
                        d = dateBefore && date - dateBefore.date > dateAfter && dateAfter.date - date ? dateAfter : dateBefore; // pick the nearest

                    tooltip += options.areaTooltipFormat(d[s.name], s.label, s.name) + '<br/>';
                    currentArea.select('circle.series-' + s.name).attr('transform', 'translate(0, ' + y(s.direction == 'down' ? -d[s.name] : d[s.name]) + ')');
                });
                
                areaTooltip.html(tooltip)
                    .css({ left: pos[0] - (areaTooltip.width() / 2), top: pos[1] - areaTooltip.height() - 20 });

                currentArea.attr('transform', 'translate(' + (pos[0]) + ', 0)');
            }

            function onWindowResized() {
                var newWidth = chart.width(),
                    newHeight = chart.height();
                if (curWidth != newWidth || curHeight != newHeight) {
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

            this.removeClass('cpu-chart memory-chart network-chart')
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