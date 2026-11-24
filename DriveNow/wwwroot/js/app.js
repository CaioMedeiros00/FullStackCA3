// Small JavaScript helpers called from Blazor through IJSRuntime.
window.driveNow = {
    getTheme: function () {
        try { return localStorage.getItem('drivenow-theme') || 'light'; } catch (e) { return 'light'; }
    },
    setTheme: function (theme) {
        document.documentElement.setAttribute('data-bs-theme', theme);
        try { localStorage.setItem('drivenow-theme', theme); } catch (e) { }
    },
    // Lets the user download generated text (CSV exports).
    downloadFile: function (fileName, content) {
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    }
};
