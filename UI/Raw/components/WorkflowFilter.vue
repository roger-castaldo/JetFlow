<template>
    <Tags :size="Sizes.normal">
        <strong>{{ translate('common.filters') }}</strong>
        <FilterTag v-if="workflowName!==null" @delete="workflowName=null">
            {{ translate('common.workflowname', { workflowname: workflowName }) }}
        </FilterTag>
        <FilterTag v-if="startedAt!==null" @delete="startedAt=null">
            {{ produceDateFilterText('startedAt', startedAt) }}
        </FilterTag>
        <FilterTag v-if="completedAt!==null" @delete="completedAt=null">
            {{ produceDateFilterText('completedAt', completedAt) }}
        </FilterTag>
        <FilterTag v-for="arg,idx in argument" :key="idx" @delete="argument.splice(idx, 1)">
            {{ produceArgumentFilterText(arg) }}
        </FilterTag>
        <template v-if="metadata!==null">
            <FilterTag v-for="metadataItem in metadata" :key="metadataItem.Key" @delete="metadata = metadata.filter(m => m !== metadataItem)">
                {{ translate('common.metadataFilter', { key: metadataItem.Key, value: metadataItem.Value }) }}
            </FilterTag>
        </template>
    </Tags>
</template>
<script setup>
    import FilterTag from '../components/FilterTag.vue';
    import {translate, formatTime} from '../i18n';
    import {Tags, Sizes} from 'components';
    import {ref, onMounted, watch} from 'vue';

    const props = defineProps({
        supportsDates: {
            type: Boolean,
            default: false
        }
    });

    const workflowName = ref("Place Order"); // Replace with the actual workflow name you want to filter by
    const startedAt = ref(null); // Replace with actual startedAt if needed
    const completedAt = ref(null); // Replace with actual completedAt if needed
    const metadata = ref(null); // Replace with actual metadata if needed
    const argument = ref([
        {"$type":"startsWith", "PropertyName":"paymentInformation.lastName", "Value":"G"}
    ]); // Replace with actual argument if needed

    const emit = defineEmits(['filterChanged']);

    watch([workflowName, startedAt, completedAt, metadata, argument], () => {
        emit('filterChanged', {
            workflowName: workflowName.value,
            startedAt,
            completedAt,
            metadata,
            argument: argument.value
        });
    });

    const produceArgumentFilterText = (arg) => {
        switch(arg.$type){
            case 'equals':
                return translate('common.argumentFilters.equals', { property: arg.PropertyName, value: arg.Value });
            case 'startsWith':
                return translate('common.argumentFilters.startsWith', { property: arg.PropertyName, value: arg.Value });
            case 'endsWith':
                return translate('common.argumentFilters.endsWith', { property: arg.PropertyName, value: arg.Value });
            default:
                return JSON.stringify(arg);
        }
    };

    const produceDateFilterText = (property, value) => {
        if (value === null) {
            return '';
        }else if (value.Start!==null && value.End!==null) {
            return translate(`common.dateFilters.{property}.between`, { property, start: formatTime(value.Start), end: formatTime(value.End) });
        }else if (value.Start!==null) {
            return translate(`common.dateFilters.{property}.after`, { property, start: formatTime(value.Start) });
        }
        return translate(`common.dateFilters.{property}.before`, { property, start: formatTime(value.End) });
    };


    onMounted(() => {
        emit('filterChanged', {
            workflowName: workflowName.value,
            startedAt,
            completedAt,
            metadata,
            argument: argument.value
        });
    });

</script>