<template>
    <Table :fixed_header="true" :full_width="true">
        <template #thead>
            <tr>
                <th>Name</th>
                <th>Scheduled At</th>
                <th>Argument</th>
                <th>MetaData</th>
                <th></th>
            </tr>
        </template>
        <template #tbody>
            <tr v-if="scheduledFlows.length===0">
                <td colspan="100%">No scheduled flows found</td>
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

    const currentNamespace = inject(Constants.namespaceName);

    const scheduledFlows = ref([]);

    const formatSchedule = (flow)=>{
        return flow.cronString ?? Constants.timeFormatter.format(new Date(flow.runsAt));
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