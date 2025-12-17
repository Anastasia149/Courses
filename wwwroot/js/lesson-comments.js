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
                html = '<div class="text-muted">Пока нет комментариев</div>';
            } else {
                comments.forEach(function(c) {
                    html += `<div class="mb-2 border rounded p-2">
                        <b>${c.userName}</b> <span class="text-muted" style="font-size:0.9em">${new Date(c.createdAt).toLocaleString()}</span><br>
                        ${escapeHtml(c.text)}
                        <div>`;
                    if (c.userId === currentUserId || isTeacher) {
                        html += `<a href="#" class="delete-comment-link text-danger me-2" data-id="${c.id}">Удалить</a>`;
                    }
                    html += `</div></div>`;
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