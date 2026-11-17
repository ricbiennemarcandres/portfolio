// =====================================================================
//  Small touches: live image preview, lightbox gallery, upper-case codes.
//  (No alert/confirm pop-ups: deleting uses its own confirmation page.)
// =====================================================================
(function () {
    "use strict";

    // ---------- Live preview of image URLs (project and image forms) ----------
    // Shows a thumbnail for every line, marks links that do not load
    // (e.g. Google Drive) and lines that repeat an earlier one.
    function renderPreview(source) {
        var target = document.querySelector(source.getAttribute("data-preview"));
        if (!target) return;

        var lines = source.value.split(/\r?\n/)
            .map(function (l) { return l.trim(); })
            .filter(function (l) { return l.length > 0; });

        target.innerHTML = "";
        var seen = {};

        lines.slice(0, 24).forEach(function (url, i) {
            var fig = document.createElement("figure");
            var img = document.createElement("img");
            var cap = document.createElement("figcaption");

            cap.textContent = "#" + (i + 1);
            if (seen[url]) {
                fig.classList.add("repeat");
                cap.textContent = "#" + (i + 1) + " repeats #" + seen[url];
            } else {
                seen[url] = i + 1;
            }

            img.alt = "";
            img.loading = "lazy";
            img.referrerPolicy = "no-referrer";
            img.onerror = function () {
                fig.classList.add("broken");
                cap.textContent = "#" + (i + 1) + " won't load";
            };
            if (/^https:\/\//i.test(url)) img.src = url;
            else img.onerror();

            fig.appendChild(img);
            fig.appendChild(cap);
            target.appendChild(fig);
        });
    }

    document.querySelectorAll("[data-preview]").forEach(function (el) {
        var timer;
        el.addEventListener("input", function () {
            clearTimeout(timer);
            timer = setTimeout(function () { renderPreview(el); }, 350);
        });
        renderPreview(el);
    });

    // ---------- Course codes in capitals as you type ----------
    document.querySelectorAll("[data-uppercase]").forEach(function (el) {
        el.addEventListener("input", function () {
            var pos = el.selectionStart;
            el.value = el.value.toUpperCase();
            el.setSelectionRange(pos, pos);
        });
    });

    // ---------- Lightbox for the project gallery ----------
    var box = document.getElementById("lightbox");
    if (box && typeof box.showModal === "function") {
        var boxImg = box.querySelector("img");
        var boxCap = box.querySelector(".lightbox-caption");

        document.querySelectorAll("[data-lightbox]").forEach(function (btn) {
            btn.addEventListener("click", function () {
                boxImg.src = btn.getAttribute("data-lightbox");
                boxCap.textContent = btn.getAttribute("data-caption") || "";
                box.showModal();
            });
        });

        // click outside the picture closes it
        box.addEventListener("click", function (e) {
            if (e.target === box) box.close();
        });
    }
})();
