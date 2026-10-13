<template>
    <Table :fixed_header="true" :full_width="true">
        <template #thead>
            <tr>
                <th>{{ translate('common.name') }}</th>
                <th>{{ translate('screens.scheduledFlows.scheduledAt') }}</th>
                <th>{{ translate('common.argument') }}</th>
                <th>{{ translate('common.metadata') }}</th>
                <th></th>
            </tr>
        </template>
        <template #tbody>
            <tr v-if="scheduledFlows.length===0">
                <td colspan="100%">{{ translate('screens.scheduledFlows.noflows') }}</td>
            </tr>
            <tr v-for="flow in scheduledFlows" :key="flow.schedulerId">
                <td>{{ flow.name }}</td>
                <td>{{ formatSchedule(flow) }}</td>
                <td>{{ shrinkString(JSON.stringify(flow.arguments, null, 2), 100) }}</td>
                <td>{{ shrinkString(JSON.stringify(flow.metaData, null, 2), 100) }}</td>
                <td></td>
            </tr>
        </template>
    </Table>
</template>

<script setup>
    import {Table} from 'components';
    import {GetScheduledFlows} from 'services';
    import {Constants, shrinkString} from 'mixins';
    import {ref, inject, onMounted} from 'vue';
    import { translate, translateCron, formatTime } from '../i18n';

    const currentNamespace = inject(Constants.namespaceName);

    const scheduledFlows = ref([]);

    const formatSchedule = (flow)=>{
        if (flow.cronString!==null){
            return translateCron(flow.cronString);
        }
        return formatTime(new Date(flow.runsAt));
    }

    onMounted(async()=>{
        try {
            const flows = await GetScheduledFlows(currentNamespace.value);
            scheduledFlows.value = flows;
        } catch (error) {
            console.error('Error loading scheduled flows:', error);
        }
    });
</script>