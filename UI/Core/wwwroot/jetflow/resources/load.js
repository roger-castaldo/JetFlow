import { Config } from 'mixins';
import { setSkin } from 'components';
import { createApp } from 'vue';
import App from 'app';

setSkin('lumen');

createApp(App).mount('#body');