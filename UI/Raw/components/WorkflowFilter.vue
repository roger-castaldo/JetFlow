<template>
    <Tags :size="Sizes.normal">
        <strong>
            {{ translate('components.workflowfilter.filters') }}
            <Icon icon="pencil" class="is-clickable" @click="showSlideout=true"/>
        </strong>
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
    <Slideout :show="showSlideout" :has_close="true" @close="showSlideout=false">
        <template #header>
            {{ translate('components.workflowfilter.title') }}
        </template>
        <template #content>
            <ComponentForm ref="filterFormRef" :elements="filterForm" @valueChanged="FilterFormChanged" style="min-width:50vw;"/>
        </template>
        <template #footer>
            <ButtonsContainer>
                <ButtonSave @click="updateFilter"/>
                <ButtonCancel @click="showSlideout=false"/>
            </ButtonsContainer>
        </template>
    </Slideout>
</template>
<script setup>
    import FilterTag from '../components/FilterTag.vue';
    import {translate, formatTime} from '../i18n';
    import {Tags, Sizes, Slideout, Icon, ComponentForm, ButtonSave, ButtonCancel, ButtonsContainer} from 'components';
    import {ref, onMounted, watch, inject, computed} from 'vue';
    import {Constants} from 'mixins';
    import {GetWorkflowNames, GetWorkflowDefinition} from 'services';

    const currentNamespace = inject(Constants.namespaceName);

    const props = defineProps({
        supportsDates: {
            type: Boolean,
            default: false
        }
    });

    const showSlideout = ref(false);
    const workflowNames = ref([]);
    const filterFormRef = ref(null);
    const workflowDefinition = ref(null);

    const workflowName = ref("Place Order"); // Replace with the actual workflow name you want to filter by
    const startedAt = ref(null); // Replace with actual startedAt if needed
    const completedAt = ref(null); // Replace with actual completedAt if needed
    const metadata = ref(null); // Replace with actual metadata if needed
    const argument = ref([
        {"$type":"startsWith", "PropertyName":"paymentInformation.lastName", "Value":"G"}
    ]); // Replace with actual argument if needed

    const filterForm = computed(() => {
        let fields = [
            {
                type: 'select',
                name: 'workflowName',
                label: translate('common.workflowname'),
                values: workflowNames.value.map(name => ({ value: name, label: name, selected: name === workflowName.value }))
            }
        ];
        if (props.supportsDates) {
            fields.push(
                {
                    type: 'date',
                    includeTime: true,
                    name: 'startedAt_min',
                    label: translate('components.workflowfilter.startedAtMin'),
                    value: (startedAt.value===null ? null : startedAt.value.Start),
                    form_columns:6
                },
                {
                    type: 'date',
                    includeTime: true,
                    name: 'startedAt_max',
                    label: translate('components.workflowfilter.startedAtMax'),
                    value: (startedAt.value===null ? null : startedAt.value.End),
                    form_columns:6
                },
                {
                    type: 'date',
                    includeTime: true,
                    name: 'completedAt_min',
                    label: translate('components.workflowfilter.completedAtMin'),
                    value: (completedAt.value===null ? null : completedAt.value.Start),
                    form_columns:6
                },
                {
                    type: 'date',
                    includeTime: true,
                    name: 'completedAt_max',
                    label: translate('components.workflowfilter.completedAtMax'),
                    value: (completedAt.value===null ? null : completedAt.value.End),
                    form_columns:6
                }
            );
        }
        if (workflowDefinition.value !== null) {
            fields.push(
                {
                    type:'subform',
                    fields: ConstructFilterFormFromWorkflowDefinition([
                        {
                            type: 'header',
                            label: translate('components.workflowfilter.argument'),
                            subtype: 'h3',
                            className:'has-text-centered'
                        }
                    ], workflowDefinition.value)
                }
            );
        }
        return fields;
    });

    const ConstructFilterFormFromWorkflowDefinition = (fields, definitionProperty, name) => {
        if (definitionProperty !== null && definitionProperty !== undefined) {
            switch (definitionProperty.$type) {
                case 'object':
                    for (const property of definitionProperty.properties) {
                        fields = ConstructFilterFormFromWorkflowDefinition(fields, property.Value, `${name===undefined ? '' : name + '.'}${property.Key}`);
                    }
                    break;
                case 'array':
                    fields = ConstructFilterFormFromWorkflowDefinition(definitionProperty.ofType, fields);
                    break;
                case 'string':
                case 'maskedString':
                    fields.push(...[
                        {
                            type: 'paragraph',
                            label: name,
                            form_columns: 6
                        },
                        {
                            type: 'select',
                            name: `${name}_filterType`,
                            values: [
                                { value: 'equals', label: translate('components.workflowfilter.argumentFilters.equals') },
                                { value: 'startsWith', label: translate('components.workflowfilter.argumentFilters.startsWith') },
                                { value: 'endsWith', label: translate('components.workflowfilter.argumentFilters.endsWith') }
                            ],
                            form_columns: 2
                        },
                        {
                            type: 'text',
                            name: `${name}_filterValue`,
                            form_columns: 4
                        }
                    ]);
                    break;
                case 'boolean':
                    break;
                case 'number':
                    break;
                case 'datetime':
                    break;
            }
        }
        return fields;
    };

    const FilterFormChanged = (event)=>{
        if (event.name === 'workflowName'){
            if (event.value === ''){
                workflowDefinition.value = null;
            }else{
                GetWorkflowDefinition(currentNamespace.value, event.value).then((definition)=>{
                    workflowDefinition.value = definition;
                });
            }
        }
        console.log(event);
    };

    const emit = defineEmits(['filterChanged']);

    const updateFilter = () => {
        const formData = filterFormRef.value.getValues();
        workflowName.value = formData.workflowName;
        startedAt.value = (formData.startedAt_min || formData.startedAt_max) ? { Start: formData.startedAt_min, End: formData.startedAt_max } : null;
        completedAt.value = (formData.completedAt_min || formData.completedAt_max) ? { Start: formData.completedAt_min, End: formData.completedAt_max } : null;
        emit('filterChanged', {
            workflowName: workflowName.value,
            startedAt: startedAt.value,
            completedAt: completedAt.value,
            metadata: metadata.value,
            argument: argument.value
        });
        showSlideout.value=false;
    };

    watch(currentNamespace, async () => {
        workflowNames.value = await GetWorkflowNames(currentNamespace.value);
    });

    const produceArgumentFilterText = (arg) => {
        switch(arg.$type){
            case 'equals':
                return translate('components.workflowfilter.argumentFilters.equals', { property: arg.PropertyName, value: arg.Value });
            case 'startsWith':
                return translate('components.workflowfilter.argumentFilters.startsWith', { property: arg.PropertyName, value: arg.Value });
            case 'endsWith':
                return translate('components.workflowfilter.argumentFilters.endsWith', { property: arg.PropertyName, value: arg.Value });
            default:
                return JSON.stringify(arg);
        }
    };

    const produceDateFilterText = (property, value) => {
        if (value === null) {
            return '';
        }else if (value.Start!==null && value.End!==null) {
            return translate(`components.workflowfilter.dateFilters.{property}.between`, { property, start: formatTime(value.Start), end: formatTime(value.End) });
        }else if (value.Start!==null) {
            return translate(`components.workflowfilter.dateFilters.{property}.after`, { property, start: formatTime(value.Start) });
        }
        return translate(`components.workflowfilter.dateFilters.{property}.before`, { property, start: formatTime(value.End) });
    };


    onMounted(async () => {
        workflowNames.value = await GetWorkflowNames(currentNamespace.value);
        emit('filterChanged', {
            workflowName: workflowName.value,
            startedAt,
            completedAt,
            metadata,
            argument: argument.value
        });
    });

</script>