import { createApp } from 'vue';
import App from './app.vue';
import { i18n } from './i18n/index.js';

export const produceApp = () => {
    let app = createApp(App);
    app.use(i18n);
    return app;
}