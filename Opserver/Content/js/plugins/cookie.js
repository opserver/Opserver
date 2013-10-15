/*!
 * jQuery Cookie Plugin v1.1
 * https://github.com/carhartl/jquery-cookie
 * Copyright 2011, Klaus Hartl
 * Dual licensed under the MIT or GPL Version 2 licenses.
 * http://www.opensource.org/licenses/mit-license.php
 * http://www.opensource.org/licenses/GPL-2.0
 */
(function(d,i){function j(c){return c}function k(c){return decodeURIComponent(c.replace(l," "))}var l=/\+/g;d.cookie=function(c,b,a){if(1<arguments.length&&(!/Object/.test(Object.prototype.toString.call(b))||null==b)){a=d.extend({},d.cookie.defaults,a);null==b&&(a.expires=-1);if("number"===typeof a.expires){var f=a.expires,e=a.expires=new Date;e.setDate(e.getDate()+f)}b=String(b);return i.cookie=[encodeURIComponent(c),"=",a.raw?b:encodeURIComponent(b),a.expires?"; expires="+a.expires.toUTCString():"",a.path?"; path="+a.path:"",a.domain?"; domain="+a.domain:"",a.secure?"; secure":""].join("")}for(var a=b||d.cookie.defaults||{},f=a.raw?j:k,e=i.cookie.split("; "),g=0,h;h=e[g]&&e[g].split("=");g++)if(f(h.shift())===c)return f(h.join("="));return null};d.cookie.defaults={}})(jQuery,document);