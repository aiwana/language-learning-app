// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Hàm để xử lý chuyển đổi giao diện sáng/tối
$(document).ready(function () {
    const currentTheme = localStorage.getItem("theme");

    if (currentTheme === "dark") {
        $("html").addClass("dark");
        $(".btn-theme-toggle .icon-moon").addClass("d-none");   
        $(".btn-theme-toggle .icon-sun").removeClass("d-none"); 
    }

    $(".btn-theme-toggle").on("click", function () {
        $("html").toggleClass("dark");
        const isDarkMode = $("html").hasClass("dark");

        if (isDarkMode) {
            $(".btn-theme-toggle .icon-moon").addClass("d-none");
            $(".btn-theme-toggle .icon-sun").removeClass("d-none");
            localStorage.setItem("theme", "dark"); 
        } else {
            $(".btn-theme-toggle .icon-moon").removeClass("d-none");
            $(".btn-theme-toggle .icon-sun").addClass("d-none");
            localStorage.setItem("theme", "light"); 
        }
    });
});