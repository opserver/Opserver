if (window.devicePixelRatio >= 2) {
    $.cookie('highDPI', 'true', { expires: 365 * 10, path: '/' });
}