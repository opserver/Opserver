// Adapted from html-query-plan: https://code.google.com/p/html-query-plan/
// Call via: $('.qp-root').drawQueryPlanLines();
// Copyright (c) 2011 Justin Pealing

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
(function ($) {
    $.fn.extend({
        drawQueryPlanLines: function() {

            if (!this.siblings().find('canvas').length) {
                this.before(
                    $('<canvas />', { css: { position: 'absolute', top: 0, left: 0 } })
                        .wrap($('<div />', { css: { position: 'relative' } }))
                        .parent()
                );
            }

            var root = this.parent(),
                canvas = root.find('canvas'),
                canvasElm = canvas[0];

            if (!canvasElm.getContext) return this;

            // The first root node may be smaller than the full query plan if using overflow
            var firstNode = $('.qp-tr', root);
            canvasElm.width = firstNode.outerWidth(true);
            canvasElm.height = firstNode.outerHeight(true);

            var offset = canvas.offset(),
                context = canvasElm.getContext('2d');
            $('.qp-tr', root).each(function() {
                var from = $('> * > .qp-node', this);
                $('> * > .qp-tr > * > .qp-node', this).each(function() {
                    /* Draws a line between two nodes.
                    context - The canvas context with which to draw.
                    offset - Canvas offset in the document.
                    from - The document jQuery object from which to draw the line.
                    to - The document jQuery object to which to draw the line. */
                    var to = $(this),
                        fromOffset = from.offset(),
                        toOffset = to.offset();
                    
                    fromOffset.top += from.outerHeight() / 2;
                    fromOffset.left += from.outerWidth();
                    toOffset.top += to.outerHeight() / 2;

                    var midOffsetLeft = fromOffset.left / 2 + toOffset.left / 2;

                    context.moveTo(fromOffset.left - offset.left, fromOffset.top - offset.top);
                    context.lineTo(midOffsetLeft - offset.left, fromOffset.top - offset.top);
                    context.lineTo(midOffsetLeft - offset.left, toOffset.top - offset.top);
                    context.lineTo(toOffset.left - offset.left, toOffset.top - offset.top);
                });
            });
            context.strokeStyle = '#666';
            context.stroke();

            return this;
        }
    });
})(jQuery);