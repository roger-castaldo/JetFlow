<template>
    <div>
        <InfiniteTable :fixed_header="true" :full_width="true" :loadMore="loadMore" :hasMore="hasMore" :loading="loading" ref="tableRef">
            <template #thead>
                <tr>
                    <th>{{ translate('common.workflowid') }}</th>
                    <th>{{ translate('common.schedulerid') }}</th>
                    <th>{{ translate('common.startedat') }}</th>
                    <th>{{ translate('common.completedat') }}</th>
                    <th>{{ translate('common.argument') }}</th>
                    <th>{{ translate('common.metadata') }}</th>
                    <th>{{ translate('common.stepcount') }}</th>
                    <th>{{ translate('screens.completedFlows.success') }}</th>
                    <th></th>
                </tr>
            </template>
            <template #tbody>
                <tr v-for="flow in flows" :key="flow.id">
                    <td>{{ flow.id }}</td>
                    <td>{{ flow.schedulerId }}</td>
                    <td>{{ formatTime(flow.startedAt) }}</td>
                    <td>{{ formatTime(flow.finishedAt) }}</td>
                    <td>{{ shrinkString(JSON.stringify(flow.arguments, null, 2), 100) }}</td>
                    <td>{{ shrinkString(JSON.stringify(flow.metaData, null, 2), 100) }}</td>
                    <td>{{ flow.steps.length }}</td>
                    <td>{{ (flow.isSuccessful ? translate('common.yes') : translate('common.no')) }}</td>
                    <td>
                        <Button icon="magnifying-glass-plus" title="Details" :size="Sizes.small" @click="currentWorkflow=flow"/>
                    </td>
                </tr>
            </template>
        </InfiniteTable>
        <WorkflowDetails :workflow="currentWorkflow" @close="currentWorkflow=null"/>
    </div>
</template>
<script setup>
    import InfiniteTable from '../components/InfiniteTable.vue';
    import WorkflowDetails from '../components/WorkflowDetails.vue';
    import {GetArchivedFlows} from 'services';
    import {Constants, shrinkString} from 'mixins';
    import {ref, inject, watch} from 'vue';
    import {Button, Sizes} from 'components';
    import {translate, formatTime} from '../i18n';

    const currentNamespace = inject(Constants.namespaceName);

    const flows = ref([]);
    const lastOffset = ref(0);
    const pageSize = 10;
    const metadata = null; // Replace with actual metadata if needed
    const argument = [
        {"$type":"startsWith", "PropertyName":"paymentInformation.lastName", "Value":"G"}
    ]; // Replace with actual argument if needed
    const currentWorkflow = ref(null);
    const hasMore = ref(true);
    const loading = ref(false);
    const tableRef = ref(null);

    const loadMore = async () => {
        loading.value=true;
        const newFlows = await GetArchivedFlows(currentNamespace.value, "Place Order", metadata, argument, lastOffset.value, pageSize);
        flows.value.push(...newFlows.data);
        lastOffset.value = newFlows.lastOffset;
        hasMore.value = (newFlows.data.length === pageSize);
        loading.value=false;
    };
    
    watch(currentNamespace,async ()=>{
        flows.value=[];
        hasMore.value=true;
        lastOffset.value=0;
        if (tableRef?.value.isAtEnd){
            await loadMore();
        }
    });

</script>