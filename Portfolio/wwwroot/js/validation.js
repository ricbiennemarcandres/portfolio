// =====================================================================
//  Browser-side rule for [ImageUrl] (Validation/ImageUrlAttribute.cs).
//  The server sends the regular expression in data-val-imageurl-pattern,
//  so the browser and Regex.IsMatch on the server use the SAME pattern.
//  Each line is checked on its own; the message names the first bad line.
//  Load this AFTER _ValidationScriptsPartial.
// =====================================================================
(function ($) {
    "use strict";
    if (!$ || !$.validator || !$.validator.unobtrusive) return;

    // Returns "" when every line is fine, otherwise the message to show.
    function firstProblem(value, params) {
        if (!value || !value.trim()) return "";

        var re = new RegExp(params.pattern, "i");
        var raw = value.split(/\r?\n/);
        var count = 0;

        for (var i = 0; i < raw.length; i++) {
            var url = raw[i].trim();
            if (url.length === 0) continue;
            count++;

            var where = params.many ? "Line " + (i + 1) : "The URL";
            if (url.length > params.max) {
                return where + " is longer than " + params.max + " characters.";
            }
            if (!re.test(url)) {
                var shown = url.length > 60 ? url.substring(0, 60) + "…" : url;
                return where + " is not a public image link: \"" + shown + "\". " +
                    "Use an https:// URL that ends in .png, .jpg, .jpeg, .gif, .webp or .svg.";
            }
        }

        if (!params.many && count > 1) return "Enter only one URL here.";
        return "";
    }

    $.validator.addMethod("imageurl", function (value, element, params) {
        return this.optional(element) || firstProblem(value, params) === "";
    });

    $.validator.unobtrusive.adapters.add("imageurl", ["pattern", "many", "max"], function (options) {
        var params = {
            pattern: options.params.pattern,
            many: options.params.many === "true",
            max: parseInt(options.params.max, 10) || 500
        };
        options.rules.imageurl = params;
        options.messages.imageurl = function () {
            return firstProblem($(options.element).val(), params) || options.message;
        };
    });
})(window.jQuery);
