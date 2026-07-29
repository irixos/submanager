const SHOW_CATEGORIES_KEY = 'submanager.subscriptions.showCategories';

export function getShowCategories() {
    return localStorage.getItem(SHOW_CATEGORIES_KEY) === 'true';
}

export function setShowCategories(value) {
    localStorage.setItem(SHOW_CATEGORIES_KEY, value);
}
