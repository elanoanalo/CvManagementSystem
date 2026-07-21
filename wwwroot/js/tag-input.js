// ===== ОБЩАЯ ЛОГИКА ДЛЯ ТЕГОВ =====
// Используется в Positions/Create, Positions/Edit, Projects/Create, Projects/Edit
//
// Как подключить на странице:
// 1. Убедись что есть элементы с такими ID:
//    - #tagsList        — контейнер куда добавляются теги
//    - #newTagInput      — поле ввода нового тега
//    - #addTagBtn         — кнопка "Добавить тег"
// 2. Подключи скрипт: <script src="~/js/tag-input.js"></script>
// 3. Вызови initTagInput() после загрузки страницы

function initTagInput(options) {
    options = options || {};
    var maxTags = options.maxTags || 10;
    var maxTagLength = options.maxTagLength || 50;
    var duplicateMessage = options.duplicateMessage || 'This tag has already been added!';
    var lengthMessage = options.lengthMessage || 'Tag is too long!';
    var maxMessage = options.maxMessage || 'Maximum number of tags reached!';

    var tagsList = document.getElementById('tagsList');
    var newTagInput = document.getElementById('newTagInput');
    var addTagBtn = document.getElementById('addTagBtn');

    if (!tagsList || !newTagInput) return; // страница без тегов — выходим

    // Собираем текущие теги (без учёта регистра) — для проверки дублей
    function getExistingTagValues() {
        return Array.from(tagsList.querySelectorAll('.badge'))
            .map(function (el) {
                // Берём только текст, без крестика удаления внутри badge
                return el.childNodes[0].textContent.trim().toLowerCase();
            });
    }

    // Пересчитываем name="Tags[i]" у всех оставшихся тегов —
    // гарантирует последовательные индексы без "дырок"
    function reindexTags() {
        var inputs = tagsList.querySelectorAll('input[type="hidden"]');
        inputs.forEach(function (input, index) {
            input.name = 'Tags[' + index + ']';
        });
    }

    function addTag() {
        var value = newTagInput.value.trim();
        if (value === '') return;

        if (value.length > maxTagLength) {
            alert(lengthMessage);
            return;
        }

        var currentCount = tagsList.querySelectorAll('.tag-item').length;
        if (currentCount >= maxTags) {
            alert(maxMessage);
            return;
        }

        // Проверка на дубликат
        if (getExistingTagValues().includes(value.toLowerCase())) {
            alert(duplicateMessage);
            return;
        }

        var div = document.createElement('div');
        div.className = 'd-inline-flex align-items-center tag-item';
        div.innerHTML =
            '<input type="hidden" name="Tags[0]" value="' + value.replace(/"/g, '&quot;') + '" />' +
            '<span class="badge bg-info text-dark me-1">' + value +
            ' <button type="button" class="btn-close btn-close-white ms-1 remove-tag-btn" style="font-size:0.6rem"></button>' +
            '</span>';

        tagsList.appendChild(div);
        reindexTags(); // сразу пересчитываем индексы после добавления

        newTagInput.value = '';
        newTagInput.focus();
    }

    if (addTagBtn) {
        addTagBtn.addEventListener('click', addTag);
    }

    newTagInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            e.preventDefault();
            addTag();
        }
    });

    // Удаление тега — делегирование событий + переиндексация
    tagsList.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-tag-btn') ||
            e.target.classList.contains('btn-close')) {
            e.target.closest('.tag-item').remove();
            reindexTags(); // пересчитываем индексы после удаления
        }
    });

    // На всякий случай — переиндексация прямо перед отправкой формы
    var form = tagsList.closest('form');
    if (form) {
        form.addEventListener('submit', reindexTags);
    }
}