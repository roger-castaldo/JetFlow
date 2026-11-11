<template>
    <div>
        <Modal name="main-modal" :display="Locked||ProgressMessage!==null">
            <div class="has-text-centered is-animated-bounce-container" v-if="ProgressMessage!=null">
                <Animation :repeating="AnimationTypes.bounce" :speed="AnimationSpeeds.slower">
                    {{AdjustedProgressMessage}}
                </Animation>
            </div>
            <Progress size="large" />
        </Modal>
        <PageNotification :visible="Message!=null||ErrorMessage!=null"
                          :type="(ErrorMessage!=null ? 'danger' : 'info')"
                          :message="(ErrorMessage!=null ? ErrorMessage : Message)"
                          :hasClose="ErrorMessage!=null"
                          :blockUser="ErrorMessage!=null" />
        <ColumnContainer :modifiers="[ColumnContainerModifiers.fullWidth, ColumnContainerModifiers.fullHeight]"
                 :columns="[{name:'menu',size:ColumnSizes.narrow},{name:'content'}]">
            <template #content>
                <div style="overflow-y:auto;height:calc(100vh - var(--bulma-column-gap) - var(--bulma-column-gap));">
                    <component :is="currentComponent"/>
                </div>
            </template>
            <template #menu>
                <Box>
                    <Title :text="translate('title')" :level="4"/>
                    <Menu>
                        <MenuList :items="mainMenu"/>
                    </Menu>
                    <Title :text="translate('namespace')" :level="6"/>
                    <Select :values="namespaces" @valueChanged="namespaceChanged"  style="margin-bottom:15px;"/>
                    <Title :text="translate('theme')" :level="6"/>
                    <Select :values="skins" @valueChanged="skinChanged"  style="margin-bottom:15px;"/>
                </Box>
            </template>
        </ColumnContainer>
    </div>
</template>

<script setup>
    import { Animation, PageNotification, Modal, Progress, AnimationTypes, AnimationSpeeds,
        ColumnContainerModifiers, ColumnContainer, ColumnSizes, Menu, MenuList,
        Title, Select, Box, AVAIABLE_SKINS, setSkin, provideLanguage
     } from 'components';
    import { Config } from 'mixins';
    import { Locked, ProgressMessage, Message, ErrorMessage, ClearProgress, Unlock, Constants} from 'mixins';
    import { onMounted, shallowRef, computed, provide, ref } from 'vue';
    import dashboard from './screens/dashboard.vue';
    import activeFlows from './screens/activeFlows.vue';
    import completedFlows from './screens/completedFlows.vue';
    import scheduledFlows from './screens/scheduledFlows.vue';
    import settings from './screens/settings.vue';
    import {GetNamespaces} from 'services';
    import { translate } from './i18n';

    const menu = [
        {
            icon:'gauge',
            title:'menu.dashboard',
            component:dashboard,
            name:'dashboard'
        },
        {
            icon:'box-open',
            title:'menu.activeflows',
            component:activeFlows,
            name:'activeflows'
        },
        {
            icon:'box-archive',
            title:'menu.completedflows',
            component:completedFlows,
            name:'completedflows'
        },
        {
            icon:'boxes-stacked',
            title:'menu.scheduledflows',
            component:scheduledFlows,
            name:'scheduledflows'
        },
        {
            icon:'cog',
            title:'menu.settings',
            component:settings,
            name:'settings'
        }
    ];

    const currentNamespace = ref(Config.defaultNamespace);

    provide('FontAwesomeCDN','https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.3.1/css/');
    provide('ChartJSCDN','https://cdn.jsdelivr.net/npm/chart.js@4.6.1/dist/');
    provide('IconSet', 'solid');
    provide('Language', 'en');
    provide(Constants.namespaceName, currentNamespace);
    provideLanguage('en', provide);

    const currentComponent = shallowRef(null);
    const currentMenu = ref('dashboard');
    const namespaces = ref([]);

    const skins = AVAIABLE_SKINS.map((s)=>({value:s,label:s,selected:s===Config.skin.toLowerCase()}));

    const mainMenu = computed(()=>{
        return menu.map(m=>{
            return {
                icon:m.icon,
                title:translate(m.title),
                onClick:()=>{
                    currentMenu.value = m.name;
                    currentComponent.value = m.component;
                },
                active: currentMenu.value===m.name
            };
        });
    });

    const AdjustedProgressMessage = computed(() => {
        if (ProgressMessage.value === null || ProgressMessage.value === undefined)
            return ProgressMessage.value;
        if (ProgressMessage.value.length <= 30)
            return ProgressMessage.value;
        return ProgressMessage.value.substring(0, 27) + '...';
    });

    const namespaceChanged = (val)=>{
        currentNamespace.value = val.value;
    };

    const skinChanged = (val)=>{
        setSkin(val.value);
    };

    onMounted(async () => {
        document.getElementById('preload-modal').remove();
        namespaces.value = (await GetNamespaces()).map((ns)=>({value:ns,label:ns,selected:ns===Config.defaultNamespace}));

        currentComponent.value = dashboard;
        ClearProgress();
        Unlock();
    });
</script>

<style>
    .is-animated-bounce-container {
        display: inline-block;
        text-transform: uppercase;
        padding-top: 25px;
        padding-bottom: 25px;
        font-size: 25px;
        font-weight: normal;
        letter-spacing: 4px;
        min-width: 12px;
        overflow: hidden;
        width: 100%;
    }
</style>