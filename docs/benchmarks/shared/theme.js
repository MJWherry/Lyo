(function () {
    var KEY = "lyo-bench-theme";

    function preferred() {
        try {
            var stored = localStorage.getItem(KEY);
            if (stored === "light" || stored === "dark") return stored;
        } catch (e) { /* ignore */ }
        return window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function apply(theme) {
        document.documentElement.setAttribute("data-theme", theme);
        var btn = document.getElementById("theme-toggle");
        if (btn) btn.textContent = theme === "dark" ? "Light" : "Dark";
    }

    function boot() {
        apply(preferred());
        var btn = document.getElementById("theme-toggle");
        if (!btn) return;
        btn.addEventListener("click", function () {
            var next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
            try { localStorage.setItem(KEY, next); } catch (e) { /* ignore */ }
            apply(next);
        });
    }

    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", boot);
    else boot();
})();
