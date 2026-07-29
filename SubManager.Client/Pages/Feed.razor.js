const VIEW_MODE_KEY = 'submanager.feed.viewMode';

export function getViewMode() {
    return localStorage.getItem(VIEW_MODE_KEY);
}

export function setViewMode(value) {
    localStorage.setItem(VIEW_MODE_KEY, value);
}
