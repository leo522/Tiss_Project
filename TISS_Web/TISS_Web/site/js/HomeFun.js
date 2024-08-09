$(document).ready(function () {
    var isLoggedIn = '@Session["LoggedIn"]' === 'True';

    if (isLoggedIn) {
        $('#logoutBtn').show(); // 顯示登出按鈕
    } else {
        $('#loginBtn').show(); // 顯示登入按鈕
    }

    if (window.location.pathname === '/Tiss/Home') {
        $('#adminUser').hide(); // 隱藏管理者按鈕
    }
});

document.addEventListener("DOMContentLoaded", function () {
    // 檢查當前頁面是否為首頁
    if (window.location.pathname === "/Home") {
        // 隱藏元素
        document.getElementById("adminUser").style.display = "none";
    }
});

$('#carouselExample').carousel({
    interval: 5000 // 5秒輪播圖
});

// Partial View控制
$('.category-button').click(function () {
    var contentTypeId = $(this).data('content-type-id');

    $.ajax({
        url: '/Tiss/GetArticles',
        type: 'GET',
        data: { contentTypeId: contentTypeId },
        success: function (data) {
            $('#articleContainer').empty();
            $('#articleContainer').append(data);
        },
        error: function (xhr, status, error) {
            console.error("讀取錯誤: " + status + " " + error);
        }
    });
});

// 影片左右跳轉
document.addEventListener('DOMContentLoaded', function () {
    var videoItems = document.querySelectorAll('.carousel-video-column .video-item');
    var articleItems = document.querySelectorAll('.carousel-article-column .article-item');
    var currentIndex = 0;

    function showItem(index) {
        videoItems.forEach(item => item.style.display = 'none');
        articleItems.forEach(item => item.style.display = 'none');

        if (videoItems[index]) videoItems[index].style.display = 'block';
        if (articleItems[index]) articleItems[index].style.display = 'block';
    }

    function showPrevious() {
        currentIndex = (currentIndex - 1 + videoItems.length) % videoItems.length;
        showItem(currentIndex);
    }

    function showNext() {
        currentIndex = (currentIndex + 1) % videoItems.length;
        showItem(currentIndex);
    }

    // 初始顯示第一個影片
    showItem(currentIndex);

    // 取得按鍵並添加事件處理程序
    var leftButton = document.querySelector('.ArrowBtn.Left');
    var rightButton = document.querySelector('.ArrowBtn.Right');

    if (leftButton) {
        leftButton.addEventListener('click', showPrevious);
    }

    if (rightButton) {
        rightButton.addEventListener('click', showNext);
    }
});
