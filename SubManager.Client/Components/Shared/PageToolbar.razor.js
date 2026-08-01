const MOBILE_QUERY = '(max-width: 599px)';
const OFFSET_PROPERTY = '--page-toolbar-offset';
const SETTLING_CLASS = 'page-toolbar-settling';

class AutoHideToolbar {
    #abortController = new AbortController();
    #element;
    #fallbackFrame = 0;
    #height = 0;
    #hiddenOffset = 0;
    #lastScrollY = 0;
    #mediaQuery = window.matchMedia(MOBILE_QUERY);
    #resizeObserver;
    #supportsScrollEnd = 'onscrollend' in window;

    constructor(element) {
        this.#element = element;
        this.#resizeObserver = new ResizeObserver(() => this.#reset());
    }

    start() {
        const options = { passive: true, signal: this.#abortController.signal };
        window.addEventListener('scroll', () => this.#onScroll(), options);

        if (this.#supportsScrollEnd)
            window.addEventListener('scrollend', () => this.#snap(), options);

        this.#mediaQuery.addEventListener('change', () => this.#reset(), {
            signal: this.#abortController.signal
        });
        this.#resizeObserver.observe(this.#element);
        this.#reset();
    }

    #onScroll() {
        if (!this.#mediaQuery.matches)
            return;

        this.#element.classList.remove(SETTLING_CLASS);
        const scrollY = Math.max(0, window.scrollY);
        this.#hiddenOffset = scrollY === 0
            ? 0
            : Math.min(this.#height, Math.max(0, this.#hiddenOffset + scrollY - this.#lastScrollY));
        this.#lastScrollY = scrollY;
        this.#applyOffset();

        if (!this.#supportsScrollEnd)
            this.#scheduleFallbackSnap();
    }

    #scheduleFallbackSnap() {
        cancelAnimationFrame(this.#fallbackFrame);
        this.#fallbackFrame = requestAnimationFrame(() => {
            this.#fallbackFrame = requestAnimationFrame(() => {
                this.#fallbackFrame = 0;
                this.#snap();
            });
        });
    }

    #snap() {
        if (!this.#mediaQuery.matches || this.#hiddenOffset === 0 || this.#hiddenOffset === this.#height)
            return;

        this.#element.classList.add(SETTLING_CLASS);
        this.#hiddenOffset = this.#hiddenOffset <= this.#height / 2 ? 0 : this.#height;
        this.#applyOffset();
    }

    #reset() {
        cancelAnimationFrame(this.#fallbackFrame);
        this.#fallbackFrame = 0;
        this.#height = this.#element.getBoundingClientRect().height;
        this.#hiddenOffset = 0;
        this.#lastScrollY = Math.max(0, window.scrollY);
        this.#element.classList.remove(SETTLING_CLASS);
        this.#element.style.removeProperty(OFFSET_PROPERTY);
    }

    #applyOffset() {
        this.#element.style.setProperty(OFFSET_PROPERTY, `${-this.#hiddenOffset}px`);
    }

    dispose() {
        this.#abortController.abort();
        this.#resizeObserver.disconnect();
        cancelAnimationFrame(this.#fallbackFrame);
        this.#element.classList.remove(SETTLING_CLASS);
        this.#element.style.removeProperty(OFFSET_PROPERTY);
    }
}

export function initialize(element) {
    const toolbar = new AutoHideToolbar(element);
    toolbar.start();
    return toolbar;
}
