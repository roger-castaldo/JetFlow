import { Config } from 'mixins';
import { setSkin } from 'components';
import {produceApp} from 'app';

setSkin(Config.skin.toLowerCase());

produceApp().mount('#body');