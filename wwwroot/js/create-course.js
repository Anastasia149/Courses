// Проверяем, что jQuery загружен
if (typeof jQuery === 'undefined') {
    console.error('jQuery is not loaded!');
} else {
    console.log('jQuery version:', jQuery.fn.jquery);
}

$(document).ready(function() {
    console.log('Create course script loaded');
    
    // Проверяем наличие элементов
    if ($('#coverUploadArea').length === 0) {
        console.error('coverUploadArea not found!');
    } else {
        console.log('coverUploadArea found');
    }
    
    if ($('#coverImageInput').length === 0) {
        console.error('coverImageInput not found!');
    } else {
        console.log('coverImageInput found');
    }
    
    // Проверяем наличие формы
    if ($('#courseForm').length === 0) {
        console.error('courseForm not found!');
    } else {
        console.log('courseForm found');
    }
    
    // Счетчик символов для краткого описания
    $('#shortDescription').on('input', function() {
        var length = $(this).val().length;
        $('#shortDescCount').text(length);
    });

    // Загрузка обложки - теперь label сам обрабатывает клик
    // Нужно только предотвратить клик на кнопке удаления
    $(document).on('click', '#removeCoverBtn, .remove-image', function(e) {
        e.preventDefault();
        e.stopPropagation();
        $('#coverImageInput').val('');
        $('.upload-placeholder').removeClass('d-none');
        $('.upload-preview').addClass('d-none');
        return false;
    });
    
    // Предотвращаем всплытие клика на превью изображении
    $(document).on('click', '#coverPreview', function(e) {
        e.stopPropagation();
    });

    $('#coverImageInput').on('change', function(e) {
        console.log('File input changed');
        var file = e.target.files[0];
        if (file) {
            console.log('File selected:', file.name, file.size);
            if (file.size > 10 * 1024 * 1024) {
                alert('Размер файла не должен превышать 10MB');
                $('#coverImageInput').val('');
                return;
            }
            var reader = new FileReader();
            reader.onload = function(e) {
                console.log('File loaded, showing preview');
                $('#coverPreview').attr('src', e.target.result);
                $('.upload-placeholder').addClass('d-none');
                $('.upload-preview').removeClass('d-none');
            };
            reader.onerror = function() {
                console.error('Error reading file');
                alert('Ошибка при чтении файла');
            };
            reader.readAsDataURL(file);
        }
    });

    $('#removeCoverBtn').on('click', function(e) {
        e.stopPropagation();
        $('#coverImageInput').val('');
        $('.upload-placeholder').removeClass('d-none');
        $('.upload-preview').addClass('d-none');
    });

    // Drag and drop для обложки
    $('#coverUploadArea').on('dragover', function(e) {
        e.preventDefault();
        $(this).addClass('dragover');
    });

    $('#coverUploadArea').on('dragleave', function(e) {
        e.preventDefault();
        $(this).removeClass('dragover');
    });

    $('#coverUploadArea').on('drop', function(e) {
        e.preventDefault();
        $(this).removeClass('dragover');
        var files = e.originalEvent.dataTransfer.files;
        if (files.length > 0) {
            $('#coverImageInput')[0].files = files;
            $('#coverImageInput').trigger('change');
        }
    });

    // Добавление категории
    var selectedCategories = [];
    $('#addCategoryBtn').on('click', function() {
        var categoryId = $('#CategoryId').val();
        var categoryText = $('#CategoryId option:selected').text();
        if (categoryId && !selectedCategories.includes(categoryId)) {
            selectedCategories.push(categoryId);
            var tag = $('<span class="category-tag">' +
                '<span>' + categoryText + '</span>' +
                '<i class="fas fa-times remove-category" data-id="' + categoryId + '"></i>' +
                '</span>');
            $('#selectedCategories').append(tag);
            $('#CategoryId').val('');
        }
    });

    $(document).on('click', '.remove-category', function() {
        var categoryId = $(this).data('id');
        selectedCategories = selectedCategories.filter(id => id !== categoryId);
        $(this).closest('.category-tag').remove();
    });

    // Сохранение выбранных категорий в скрытое поле перед отправкой
    $('#courseForm').on('submit', function(e) {
        console.log('Form submitting...');
        console.log('Form action:', $(this).attr('action'));
        console.log('Form method:', $(this).attr('method'));
        
        // Сохраняем выбранные категории
        $('#SelectedCategories').val(selectedCategories.join(','));
        
        // Проверяем, что название курса заполнено
        var title = $('#Title').val();
        console.log('Title value:', title);
        if (!title || title.trim() === '') {
            e.preventDefault();
            e.stopPropagation();
            alert('Пожалуйста, заполните название курса');
            return false;
        }
        
        // Если выбраны категории, используем первую как основную
        if (selectedCategories.length > 0 && !$('#CategoryId').val()) {
            $('#CategoryId').val(selectedCategories[0]);
        }
        
        // Проверяем наличие файла обложки
        var coverFile = $('#coverImageInput')[0].files[0];
        if (coverFile) {
            console.log('Cover image file:', coverFile.name, coverFile.size);
        } else {
            console.log('No cover image selected (this is OK)');
        }
        
        // Проверяем все поля формы
        var formData = new FormData(this);
        console.log('Form data:');
        for (var pair of formData.entries()) {
            if (pair[0] !== 'CoverImage') {
                console.log(pair[0] + ': ' + pair[1]);
            } else {
                console.log(pair[0] + ': [FILE]');
            }
        }
        
        console.log('Form validation passed, submitting...');
        // Не предотвращаем отправку формы - пусть отправляется
        return true;
    });

    // Сохранение черновика
    $('#saveDraftBtn').on('click', function() {
        // TODO: Реализовать сохранение черновика
        alert('Черновик сохранен');
    });

    // Предпросмотр
    $('#previewBtn').on('click', function() {
        // TODO: Реализовать предпросмотр
        alert('Предпросмотр будет доступен после создания курса');
    });
});

