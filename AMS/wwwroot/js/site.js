// ==========================================================================
// AMS Emergency Coordination - Interactive UI Enhancements
// ==========================================================================

document.addEventListener("DOMContentLoaded", function () {
    // 1. Highlight Active Nav Links dynamically
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll(".ams-nav-link");

    navLinks.forEach(link => {
        const href = link.getAttribute("href");
        if (href && href !== "/" && currentPath.includes(href.toLowerCase())) {
            link.classList.add("active");
        } else if (href === "/" && (currentPath === "/" || currentPath.endsWith("/home") || currentPath.endsWith("/home/index"))) {
            link.classList.add("active");
        }
    });

    // 2. Initialize Bootstrap Tooltips if available
    if (typeof bootstrap !== "undefined" && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // 3. Auto-dismiss non-critical alerts after 8 seconds
    const autoDismissAlerts = document.querySelectorAll(".alert.alert-success");
    autoDismissAlerts.forEach(alertEl => {
        setTimeout(() => {
            if (typeof bootstrap !== "undefined" && bootstrap.Alert) {
                const bsAlert = new bootstrap.Alert(alertEl);
                bsAlert.close();
            }
        }, 8000);
    });

    // 4. Subtle ripple/click feedback on buttons
    const interactiveButtons = document.querySelectorAll(".btn-ams-primary, .ams-back-btn, .btn-primary");
    interactiveButtons.forEach(btn => {
        btn.addEventListener("click", function () {
            this.style.transform = "scale(0.98)";
            setTimeout(() => {
                this.style.transform = "";
            }, 150);
        });
    });
});
