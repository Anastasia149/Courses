$(document).ready(function() {
    if (typeof lessonIdForHomework === 'undefined') return;
    let currentHomeworkId = null;
    $.get('/Homework/GetHomework', { lessonId: lessonIdForHomework }, function(homework) {
        // Проверяем, что задание существует, не отменено и имеет непустой ответ
        // Задания с пустым ответом (созданные автоматически для комментариев) не считаются отправленными
        if (homework && homework.status !== 'Cancelled' && homework.answer && homework.answer.trim() !== '' && homework.status !== 'Cancelled') {
            currentHomeworkId = homework.id;
            
            // Отображаем статус
            var statusBadge = $('#statusBadge');
            statusBadge.removeClass('pending approved rejected');
            
            var statusText = '';
            var statusClass = '';
            switch(homework.status) {
                case 'Pending':
                    statusText = 'Ожидает проверки';
                    statusClass = 'pending';
                    break;
                case 'Approved':
                    statusText = 'Принято';
                    statusClass = 'approved';
                    break;
                case 'Rejected':
                    statusText = 'Требует доработки';
                    statusClass = 'rejected';
                    break;
                default:
                    statusText = 'Ожидает проверки';
                    statusClass = 'pending';
            }
            statusBadge.addClass(statusClass).text(statusText);
            
            // Отображаем отзыв преподавателя
            if (homework.feedback && homework.feedback.trim() !== '') {
                $('#instructorFeedback').text(homework.feedback);
                $('#instructorFeedbackSection').show();
            } else {
                $('#instructorFeedbackSection').hide();
            }
            
            // Отображаем отправленную работу
            $('#submittedWorkAnswer').text(homework.answer || '');
            
            // Отображаем файлы
            if (homework.files && homework.files.length > 0) {
                var filesHtml = '';
                homework.files.forEach(function(file) {
                    filesHtml += '<div class="file-attachment">' +
                        '<i class="fas fa-paperclip"></i>' +
                        '<a href="/Homework/DownloadFile/' + file.id + '" target="_blank">' + file.fileName + '</a>' +
                        '</div>';
                });
                $('#submittedFiles').html(filesHtml).show();
            } else {
                $('#submittedFiles').hide();
            }
            
            // Отображаем дату отправки
            if (homework.submittedAt) {
                var date = new Date(homework.submittedAt);
                var dateStr = date.toLocaleDateString('ru-RU', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric'
                });
                var timeStr = date.toLocaleTimeString('ru-RU', {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
                $('#submissionDateText').text('Отправлено: ' + dateStr + ', ' + timeStr);
            }
            
            $('#submittedHomework').show();
            
            // Если задание принято или ожидает проверки, скрываем форму
            if (homework.status === 'Approved' || homework.status === 'Pending') {
                $('#homeworkForm').hide();
                $('#cancelButton').show();
            } else if (homework.status === 'Rejected') {
                // Если отклонено, показываем форму для повторной отправки
                // Заполняем форму данными из отправленного задания
                $('#homeworkForm').show();
                $('#answer').val(homework.answer || '');
                $('#cancelButton').show();
            } else {
                // Если статус Cancelled или другой - показываем форму
                $('#submittedHomework').hide();
                $('#homeworkForm').show();
                $('#cancelButton').hide();
            }
        } else {
            // Задания нет, оно отменено или пустое - показываем форму
            $('#submittedHomework').hide();
            $('#homeworkForm').show();
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