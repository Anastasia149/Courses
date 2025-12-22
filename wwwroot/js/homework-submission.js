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
        
        // Убеждаемся, что форма видна после загрузки данных
        console.log('Форма видима после загрузки:', $('#homeworkForm').is(':visible'));
        console.log('Кнопка видима после загрузки:', $('#submitButton').is(':visible'));
    });

    // Обработка отправки формы - привязываем сразу, независимо от состояния формы
    console.log('Привязка обработчика submit для формы homeworkForm...');
    var homeworkForm = $('#homeworkForm');
    console.log('Форма найдена:', homeworkForm.length > 0);
    if (homeworkForm.length > 0) {
        console.log('Форма видима:', homeworkForm.is(':visible'));
        console.log('Форма display:', homeworkForm.css('display'));
        console.log('Форма action:', homeworkForm.attr('action'));
    }
    
    // Проверяем кнопку submit
    var submitButton = $('#submitButton');
    console.log('Кнопка найдена:', submitButton.length > 0);
    if (submitButton.length > 0) {
        console.log('Кнопка видима:', submitButton.is(':visible'));
        console.log('Кнопка type:', submitButton.attr('type'));
    }
    
    // Также добавляем обработчик на кнопку для отладки - используем capture фазу
    document.addEventListener('click', function(e) {
        if (e.target && (e.target.id === 'submitButton' || e.target.closest('#submitButton'))) {
            console.log('Кнопка submit нажата (через capture)!');
            console.log('Target:', e.target);
            console.log('Форма в момент клика:', $('#homeworkForm').is(':visible'));
        }
    }, true);
    
    $(document).on('click', '#submitButton', function(e) {
        console.log('Кнопка submit нажата (через jQuery)!');
        console.log('Event:', e);
        console.log('Форма в момент клика:', $('#homeworkForm').is(':visible'));
        console.log('Кнопка в момент клика:', $(this).is(':visible'));
        
        // Проверяем файлы перед отправкой
        var fileInput = document.getElementById('files');
        if (fileInput) {
            console.log('fileInput.files в момент клика:', fileInput.files);
            console.log('fileInput.files.length в момент клика:', fileInput.files ? fileInput.files.length : 0);
        }
        
        // Если форма скрыта, показываем её
        if (!$('#homeworkForm').is(':visible')) {
            console.log('Форма была скрыта, показываем её');
            $('#homeworkForm').show();
        }
        
        // Принудительно обновляем файлы перед отправкой
        if (typeof window.updateFileInput === 'function') {
            console.log('Принудительно обновляем файлы перед отправкой...');
            try {
                window.updateFileInput();
                console.log('Файлы обновлены, теперь fileInput.files.length:', fileInput ? fileInput.files.length : 0);
            } catch (err) {
                console.error('Ошибка при обновлении файлов:', err);
            }
        }
        
        // Не предотвращаем default, чтобы форма могла отправиться естественным образом
        // Но также добавляем небольшую задержку для синхронизации файлов
        setTimeout(function() {
            console.log('Триггерим submit после обновления файлов...');
            $('#homeworkForm').trigger('submit');
        }, 50);
    });
    
    // Используем делегирование событий для надежности
    $(document).on('submit', '#homeworkForm', function(e) {
        console.log('Обработчик submit сработал!');
        e.preventDefault();
        e.stopPropagation();
        
        // Помечаем, что форма отправляется
        $(this).data('submitted', true);
        
        var form = $(this);
        
        // Валидация: требуется текстовый ответ
        var answer = form.find('#answer').val() || '';
        var fileInput = document.getElementById('files');
        
        if (!answer || !answer.trim()) {
            alert('Пожалуйста, введите текстовый ответ.');
            return false;
        }
        
        var formData = new FormData();
        
        // Добавляем lessonId
        var lessonId = form.find('input[name="lessonId"]').val();
        formData.append('lessonId', lessonId);
        console.log('LessonId:', lessonId);
        
        // Добавляем answer (текстовый ответ) - даже если пустой
        formData.append('answer', answer || '');
        console.log('Answer:', answer || '(пусто)');
        
        // Добавляем файлы из input (если есть)
        if (fileInput && fileInput.files && fileInput.files.length > 0) {
            for (var i = 0; i < fileInput.files.length; i++) {
                formData.append('files', fileInput.files[i]);
            }
        }
        
        // Добавляем anti-forgery token, если он есть
        var token = form.find('input[name="__RequestVerificationToken"]').val();
        if (token) {
            formData.append('__RequestVerificationToken', token);
        }
        
        // Логируем содержимое FormData для отладки
        console.log('Отправка AJAX запроса...');
        console.log('FormData содержимое:');
        for (var pair of formData.entries()) {
            if (pair[1] instanceof File) {
                console.log(pair[0] + ': [FILE] ' + pair[1].name + ' (' + pair[1].size + ' bytes)');
            } else {
                console.log(pair[0] + ': ' + pair[1]);
            }
        }
        
        $.ajax({
            url: form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function(response) {
                console.log('Успешная отправка:', response);
                window.location.reload();
            },
            error: function(xhr, status, error) {
                console.error('Ошибка при отправке:', error);
                console.error('Status:', status);
                console.error('Response:', xhr.responseText);
                console.error('Status Code:', xhr.status);
                
                // Показываем более детальную информацию об ошибке
                var errorMessage = 'Произошла ошибка при отправке домашнего задания.';
                if (xhr.responseText) {
                    try {
                        var response = JSON.parse(xhr.responseText);
                        if (response.error || response.message) {
                            errorMessage = response.error || response.message;
                        }
                    } catch (e) {
                        // Если не JSON, показываем текст ответа
                        if (xhr.responseText.length < 500) {
                            errorMessage += '\n' + xhr.responseText;
                        }
                    }
                }
                alert(errorMessage);
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