let currentLessonIdForComments = null;

$(function() {
    $('.view-comments-btn').on('click', function() {
        const lessonId = $(this).data('lesson-id');
        const lessonTitle = $(this).data('lesson-title');
        currentLessonIdForComments = lessonId;
        $('#commentsModalTitle').text('Комментарии к уроку: ' + lessonTitle);
        window.lessonIdForComments = lessonId;
        $('#commentsModal').modal('show');
        loadComments();
    });

    function loadComments() {
        if (!currentLessonIdForComments) return;
        $.get('/LessonComment/GetComments', { lessonId: currentLessonIdForComments }, function(data) {
            var comments = data.comments;
            var currentUserId = data.currentUserId;
            var isTeacher = data.isTeacher;
            var html = '';
            comments.forEach(function(c) {
                html += `<div class="mb-2 border rounded p-2">
                    <b>${c.userName}</b> <span class="text-muted" style="font-size:0.9em">${new Date(c.createdAt).toLocaleString()}</span><br>
                    ${escapeHtml(c.text)}
                    <div>`;
                if (c.userId === currentUserId || isTeacher) {
                    html += `<a href=\"#\" class=\"delete-comment-link text-danger me-2\" data-id=\"${c.id}\">Удалить</a>`;
                }
                html += `<a href="#" class="reply-link" data-id="${c.id}">Ответить</a>`;
                html += `</div>`;
                if (c.replies && c.replies.length) {
                    c.replies.forEach(function(r) {
                        html += `<div class=\"ms-4 mt-2 border-start ps-2\">
                            <b>${r.userName}</b> <span class=\"text-muted\" style=\"font-size:0.9em\">${new Date(r.createdAt).toLocaleString()}</span><br>
                            ${escapeHtml(r.text)}
                            <div>`;
                        if (r.userId === currentUserId || isTeacher) {
                            html += `<a href=\"#\" class=\"delete-comment-link text-danger me-2\" data-id=\"${r.id}\">Удалить</a>`;
                        }
                        html += `</div>`;
                        html += `</div>`;
                    });
                }
                html += `</div>`;
            });
            $('#commentsList').html(html);
        });
    }

    $('#addCommentForm').on('submit', function(e) {
        e.preventDefault();
        var text = $('#commentText').val();
        var parentId = $('#parentCommentId').val() || null;
        $.post('/LessonComment/AddComment', { lessonId: currentLessonIdForComments, text: text, parentCommentId: parentId }, function() {
            $('#commentText').val('');
            $('#parentCommentId').val('');
            $('#cancelReply').hide();
            loadComments();
        });
    });

    $('#commentsList').on('click', '.reply-link', function(e) {
        e.preventDefault();
        var id = $(this).data('id');
        $('#parentCommentId').val(id);
        $('#commentText').focus();
        $('#cancelReply').show();
    });

    $('#cancelReply').on('click', function() {
        $('#parentCommentId').val('');
        $(this).hide();
    });

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