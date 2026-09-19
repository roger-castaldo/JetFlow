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
        <ColumnContainer :modifiers="[ColumnContainerModifiers.fullWidth, ColumnContainerModifiers.fullHeight,ColumnContainerModifiers.gapless]"
                 :columns="[{name:'menu',size:ColumnSizes.narrow,border:[BorderTypes.right]},{name:'content'}]">
            <template #content>
                <component :is="currentComponent"/>
            </template>
            <template #menu>
                <Title text="JETFLOW" :level="4"/>
                <Menu>
                    <MenuList :items="mainMenu"/>
                </Menu>
            </template>
        </ColumnContainer>
    </div>
</template>

<script setup>
    import { Animation, PageNotification, Modal, Progress, AnimationTypes, AnimationSpeeds,
        ColumnContainerModifiers, ColumnContainer, ColumnSizes, BorderTypes, Menu, MenuList,
        Title
     } from 'components';
    import { Locked, ProgressMessage, Message, ErrorMessage, ClearProgress, Unlock} from 'mixins';
    import { onMounted, shallowRef, computed, provide, ref } from 'vue';
    import dashboard from './screens/dashboard.vue';
    import activeFlows from './screens/activeFlows.vue';
    import completedFlows from './screens/completedFlows.vue';
    import scheduledFlows from './screens/scheduledFlows.vue';
    import settings from './screens/settings.vue';

    const menu = [
        {
            icon:'gauge',
            title:'Dashboard',
            component:dashboard,
            name:'dashboard'
        },
        {
            icon:'box-open',
            title:'Active Flows',
            component:activeFlows,
            name:'activeflows'
        },
        {
            icon:'box-archive',
            title:'Completed Flows',
            component:completedFlows,
            name:'completedflows'
        },
        {
            icon:'boxes-stacked',
            title:'Scheduled Flows',
            component:scheduledFlows,
            name:'scheduledflows'
        },
        {
            icon:'cog',
            title:'Settings',
            component:settings,
            name:'settings'
        }
    ];
    provide('FontAwesomeCDN','https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.3.1/css/');
    provide('IconSet', 'solid');
    provide('Language', 'en');

    const currentComponent = shallowRef(null);
    const currentMenu = ref('dashboard');

    const mainMenu = computed(()=>{
        return menu.map(m=>{
            return {
                icon:m.icon,
                title:m.title,
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

    onMounted(async () => {
        document.getElementById('preload-modal').remove();
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