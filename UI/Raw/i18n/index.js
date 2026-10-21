import { createI18n } from 'vue-i18n';
import cronstrue from 'cronstrue/i18n';
import { computed, ref } from 'vue';

import en from './messages/en.json'
import fr from './messages/fr.json'

export const supportedLocales = ['en', 'fr'];

const savedLocale = localStorage.getItem('locale');

const locale = ref(savedLocale && supportedLocales.includes(savedLocale)
  ? savedLocale
  : 'en');

export const i18n = createI18n({
  legacy: false,       // Composition API
  locale,
  fallbackLocale: 'en',
  messages: {
    en,
    fr
  }
})

export function setLocale(local) {
  if (supportedLocales.indexOf(local)<0){
    local = 'en';
  }
  locale.value = local;
  i18n.global.locale.value = local;
  document.documentElement.lang = local;
  localStorage.setItem('locale', local);
}

export function getLocale(){
  return locale.value;
}

const timeFormatter = computed(()=>{
  let local = 'en-US';
  if (locale.value!=='en'){
    local = `${locale.value}-${locale.value.toUpperCase()}`;
  }
  return new Intl.DateTimeFormat(local, { dateStyle: 'short', timeStyle: 'short' });
});

export const translate = (message, args) => (args===undefined ? i18n.global.t(message) : i18n.global.t(message,args));
export const translateCron = (cron) => cronstrue.toString(cron, { locale: locale.value });

export const formatTime = (date) => {
  if (!(date instanceof Date && !isNaN(date))){
    date=new Date(date);
  }
  return timeFormatter.value.format(date);
}