// ОБЩАЯ ЛОГИКА ДЛЯ ТЕГОВ

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

    if (!tagsList || !newTagInput) return;

    function getExistingTagValues() {
        return Array.from(tagsList.querySelectorAll('.badge'))
            .map(function (el) {
                return el.childNodes[0].textContent.trim().toLowerCase();
            });
    }

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
        reindexTags();

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

    tagsList.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-tag-btn') ||
            e.target.classList.contains('btn-close')) {
            e.target.closest('.tag-item').remove();
            reindexTags();
        }
    });

    var form = tagsList.closest('form');
    if (form) {
        form.addEventListener('submit', reindexTags);
    }
}