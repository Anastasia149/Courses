$(document).ready(function() {
    if (typeof lessonIdForHomework === 'undefined') return;
    let currentHomeworkId = null;
    $.get('/Homework/GetHomework', { lessonId: lessonIdForHomework }, function(homework) {
        if (homework) {
            currentHomeworkId = homework.id;
            // Показываем отправленное задание
            $('#homeworkAnswer').html('<strong>Ответ:</strong><br>' + homework.answer);
            if (homework.files && homework.files.length > 0) {
                var filesHtml = '';
                homework.files.forEach(function(file) {
                    filesHtml += '<li class="list-group-item d-flex justify-content-between align-items-center">' +
                        '<a href="/Homework/DownloadFile/' + file.id + '" target="_blank">' + file.fileName + '</a>' +
                        '<span class="badge bg-secondary rounded-pill">' + formatFileSize(file.fileSize) + '</span>' +
                        '</li>';
                });
                $('#filesList').html(filesHtml);
                $('#homeworkFiles').show();
            } else {
                $('#homeworkFiles').hide();
            }
            if (homework.feedback) {
                $('#homeworkFeedback').html('<strong>Комментарий преподавателя:</strong><br>' + homework.feedback).show();
            } else {
                $('#homeworkFeedback').hide();
            }
            $('#submittedHomework').show();
            $('#homeworkForm').hide();
            // Кнопка отмены всегда видна, если есть домашка
            $('#cancelButton').show();
        } else {
            $('#submittedHomework').hide();
            $('#homeworkForm').show();
            // Кнопка отмены скрыта, если домашки нет
            $('#cancelButton').hide();
        }
    });

    // Обработка отправки формы
    $('#homeworkForm').on('submit', function(e) {
        e.preventDefault();
        var form = $(this);
        var formData = new FormData(form[0]);
        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function() {
                window.location.reload();
            },
            error: function() {
                alert('Произошла ошибка при отправке домашнего задания.');
            }
        });
    });

    // Обработка отмены
    $('#cancelButton').on('click', function() {
        if (confirm('Вы уверены, что хотите отменить отправку домашнего задания?')) {
            if (!currentHomeworkId) {
                alert('Ошибка: не найден идентификатор домашнего задания.');
                return;
            }
            $.post('/Homework/Cancel', { homeworkId: currentHomeworkId }, function() {
                window.location.reload();
            }).fail(function() {
                alert('Произошла ошибка при отмене домашнего задания.');
            });
        }
    });

    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }
}); 