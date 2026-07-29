const VIEW_MODE_KEY = 'submanager.feed.viewMode';
const SHOW_CATEGORIES_KEY = 'submanager.feed.showCategories';
const ON_PULL_TO_REFRESH = 'OnPullToRefresh';

let pullToRefresh;

export function getViewMode() {
    return localStorage.getItem(VIEW_MODE_KEY);
}

export function setViewMode(value) {
    localStorage.setItem(VIEW_MODE_KEY, value);
}

export function getShowCategories() {
    return localStorage.getItem(SHOW_CATEGORIES_KEY) === 'true';
}

export function setShowCategories(value) {
    localStorage.setItem(SHOW_CATEGORIES_KEY, value);
}

class PullToRefresh {
    #abortController = new AbortController();
    #distance = 0;
    #dotNetRef;
    #element;
    #startY = 0;
    #tracking = false;

    constructor(element, dotNetRef) {
        this.#element = element;
        this.#dotNetRef = dotNetRef;
    }

    start() {
        const options = { signal: this.#abortController.signal };
        document.addEventListener('touchstart', event => this.#start(event), options);
        document.addEventListener('touchmove', event => this.#move(event), {
            ...options,
            passive: false
        });
        document.addEventListener('touchend', () => this.#finish(), options);
        document.addEventListener('touchcancel', () => this.#reset(), options);
    }

    #start(event) {
        if (window.scrollY > 0 || event.touches.length !== 1)
            return;

        this.#startY = event.touches[0].clientY;
        this.#tracking = true;
    }

    #move(event) {
        if (!this.#tracking)
            return;

        this.#distance = Math.min(96, Math.max(0, event.touches[0].clientY - this.#startY) * 0.55);
        if (this.#distance === 0)
            return;

        event.preventDefault();
        this.#element.style.setProperty('--pull-distance', `${this.#distance}px`);
        this.#element.classList.add('pull-refresh-active');
        this.#element.classList.toggle('pull-refresh-ready', this.#distance >= 64);
    }

    async #finish() {
        if (!this.#tracking)
            return;

        const shouldRefresh = this.#distance >= 64;
        this.#reset();
        if (!shouldRefresh)
            return;

        try {
            await this.#dotNetRef.invokeMethodAsync(ON_PULL_TO_REFRESH);
        } catch {
            // The component was disposed while the callback was in flight.
        }
    }

    #reset() {
        this.#tracking = false;
        this.#distance = 0;
        this.#element.style.removeProperty('--pull-distance');
        this.#element.classList.remove('pull-refresh-active', 'pull-refresh-ready');
    }

    dispose() {
        this.#abortController.abort();
        this.#reset();
    }
}

export function initializePullToRefresh(element, dotNetRef) {
    pullToRefresh = new PullToRefresh(element, dotNetRef);
    pullToRefresh.start();
}

export function dispose() {
    pullToRefresh?.dispose();
    pullToRefresh = undefined;
}
