const SHOW_CATEGORIES_KEY = 'submanager.subscriptions.showCategories';
const VIEW_MODE_KEY = 'submanager.subscriptions.viewMode';

export function getPreferences() {
    return {
        showCategories: localStorage.getItem(SHOW_CATEGORIES_KEY) === 'true',
        gridView: localStorage.getItem(VIEW_MODE_KEY) === 'grid'
    };
}

export function setShowCategories(value) {
    localStorage.setItem(SHOW_CATEGORIES_KEY, value);
}

export function setGridView(value) {
    localStorage.setItem(VIEW_MODE_KEY, value ? 'grid' : 'list');
}
