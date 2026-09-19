import { readonly, shallowRef, nextTick, watch, computed, defineComponent, cloneVNode } from 'vue';

export const css = (val) => {
    if (!Array.isArray(val)) {
        val = [val];
    }
    return new Promise((resolve) => {
        let promises = [];
        let head = document.head || document.getElementsByTagName('head')[0];
        val.forEach((url,index) => {
            if (!url.toLowerCase().endsWith('.css')) {
                url += '.css';
            }
            let add = document.querySelectorAll('link[server_path="' + url + '"]').length == 0;
            if (add) {
                let style = document.createElement('link');
                let prom = new Promise(resolve => {
                    style.onload = function () { resolve(url); };
                });
                promises[index] = prom;
                head.appendChild(style);
                style.setAttribute('rel', 'stylesheet');
                style.setAttribute('type', 'text/css');
                style.setAttribute('server_path', url);
                style.setAttribute('href', fixPath(url));
            } else
                promises[index] = Promise.resolve(url);
        });
        Promise.all(promises).then(results => {
            resolve(results);
        });
    });
}

const _progressMessage = shallowRef('Loading...');
const _message = shallowRef(null);
const _errorMessage = shallowRef(null);
const _locks = shallowRef(0);

watch(_locks, (newValue) => {
    if (newValue === -1) {
        if (_progressMessage.value != null) {
            _progressMessage.value = null;
        }
    }
});

watch(_message, (newValue) => {
    if (newValue != null) {
        nextTick(function () {
            setTimeout(function () {
                _message.value = null;
            }, 10000);
        });
    }
});

watch(_errorMessage, (newValue) => {
    if (newValue != null) {
        nextTick(function () {
            setTimeout(function () {
                _errorMessage.value = null;
            }, 10000);
        });
    }
});

const mobileQuery = 'screen and (max-width: 1023px)';
const desktopQuery = 'screen and (min-width: 1024px)';

const _isMobile = shallowRef(window.matchMedia(mobileQuery).matches);
const _isDesktop = shallowRef(window.matchMedia(desktopQuery).matches);

window.addEventListener("resize", () => {
    _isMobile.value = window.matchMedia(mobileQuery).matches;
    _isDesktop.value = window.matchMedia(desktopQuery).matches;
});

export const Config = readonly({
    IsMobile: readonly(_isMobile),
    IsDesktop: readonly(_isDesktop)
});
export const Locked = computed(() => _locks.value!==-1);
export const ProgressMessage = readonly(_progressMessage);
export const Message = readonly(_message);
export const ErrorMessage = readonly(_errorMessage);

export const Lock = () => {
    _locks.value = Math.max(1, _locks.value + 1);
    return new Promise(resolve => {
        if ($.find('[name="main-modal"].is-active').length != 0) {
            _locks.value--;
            nextTick(() => {
                resolve();
            });
        } else {
            let intv = setInterval(function () {
                if ($.find('[name="main-modal"].is-active').length != 0) {
                    clearInterval(intv);
                    _locks.value--;
                    nextTick(() => {
                        resolve();
                    });
                }
            }, 200);
        }
    });
};

export const Unlock = () => {
    if (_locks.value === 0) {
        _locks.value = -1
    }
};

export const UserLoggedIn = (user) => {
    if (_user.value !== null)
        throw 'Unable to set session user';
    _user.value = user;
};



export const ClearProgress = () => _progressMessage.value = null;
export const SetProgress = (message) => {
    _progressMessage.value = message;
    return new Promise(resolve => {
        nextTick(() => {
            resolve();
        });
    });
};

export const ShowMessage = (message) =>_message.value = message;
export const ShowErrorMessage = () =>_errorMessage.value = message;

export const SlotRef = defineComponent({
    name: 'SlotRef',
    props: {
        assignRef: { type: Function }
    },
    setup(props, { slots }) {
        return () => {
            const vnodes = slots.default ? slots.default() : [];
            if (vnodes.length === 1 && props.assignRef) {
                return cloneVNode(vnodes[0], { ref: props.assignRef })
            }
            return vnodes
        }
    }
});