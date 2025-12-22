$(function() {
    var lessonId = window.lessonIdForComments;
    if (!lessonId) return;

    function loadComments() {
        $.get('/LessonComment/GetComments', { lessonId: lessonId }, function(data) {
            var comments = data.comments;
            var currentUserId = data.currentUserId;
            var isTeacher = data.isTeacher;
            var html = '';
            
            if (comments.length === 0) {
                html = '<div class="text-muted text-center py-3">Пока нет комментариев. Будьте первым!</div>';
            } else {
                comments.forEach(function(c) {
                    html += `<div class="comment-item">
                        <div class="comment-header">
                            <span class="comment-author">${escapeHtml(c.userName)}</span>
                            <span class="comment-date">${new Date(c.createdAt).toLocaleString('ru-RU')}</span>
                        </div>
                        <div class="comment-text">${escapeHtml(c.text)}</div>`;
                    if (c.userId === currentUserId || isTeacher) {
                        html += `<div class="comment-actions">
                            <a href="#" class="delete-comment-link text-danger" data-id="${c.id}">
                                <i class="fas fa-trash me-1"></i>Удалить
                            </a>
                        </div>`;
                    }
                    html += `</div>`;
                });
            }
            
            $('#commentsList').html(html);
        });
    }

    loadComments();

    $('#addCommentForm').on('submit', function(e) {
        e.preventDefault();
        var text = $('#commentText').val();
        if (!text || text.trim() === '') {
            alert('Введите комментарий');
            return;
        }
        $.post('/LessonComment/AddComment', { lessonId: lessonId, text: text }, function() {
            $('#commentText').val('');
            loadComments();
        }).fail(function() {
            alert('Ошибка при добавлении комментария. Убедитесь, что вы отправили задание.');
        });
    });

    // Удаление комментария
    $('#commentsList').on('click', '.delete-comment-link', function(e) {
        e.preventDefault();
        if (!confirm('Удалить комментарий?')) return;
        var id = $(this).data('id');
        $.post('/LessonComment/DeleteComment', { commentId: id }, function() {
            loadComments();
        });
    });

    function escapeHtml(text) {
        return $('<div>').text(text).html();
    }
}); 