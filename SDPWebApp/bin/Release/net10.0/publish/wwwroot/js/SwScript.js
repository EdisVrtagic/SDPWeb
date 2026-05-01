var statsSwiper = new Swiper(".statsSwiper", {
    slidesPerView: 2,
    spaceBetween: 20,
    pagination: { el: ".swiper-pagination", clickable: true },
    breakpoints: {
        576: { slidesPerView: 2 },
        992: { slidesPerView: 5 }
    }
});
var swiper = new Swiper(".mySwiper", {
    slidesPerView: 1,
    spaceBetween: 20,
    pagination: { el: ".swiper-pagination", clickable: true },
    breakpoints: {
        576: { slidesPerView: 2 },
        992: { slidesPerView: 3 },
        1400: { slidesPerView: 5 }
    }
});
function autoUpload() {
    const fileInput = document.getElementById('fileInput');
    if (fileInput.files.length > 0) {
        document.getElementById('uploadForm').submit();
    }
}