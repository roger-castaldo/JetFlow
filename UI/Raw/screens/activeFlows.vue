<template>
    <div>
        <InfiniteTable :fixed_header="true" :full_width="true" :loadMore="loadMore">
            <template #thead>
                <tr>
                    <th>{{ translate('common.schedulerid') }}</th>
                    <th>{{ translate('common.startedat') }}</th>
                    <th>{{ translate('common.oncomplete') }}</th>
                    <th>{{ translate('common.onactivitytimeout') }}</th>
                    <th>{{ translate('common.onactivityerror') }}</th>
                    <th>{{ translate('common.argument') }}</th>
                    <th>{{ translate('common.metadata') }}</th>
                    <th>{{ translate('screens.activeFlows.stepcount') }}</th>
                    <th>{{ translate('screens.activeFlows.currentstep') }}</th>
                    <th></th>
                </tr>
            </template>
            <template #tbody>
                <tr v-for="flow in flows" :key="flow.ID">
                    <td>{{ flow.schedulerId }}</td>
                    <td>{{ formatTime(flow.startedAt) }}</td>
                    <td>{{ flow.options.completionAction }}{{ flow.options.purgeDelay!==null ? translate('screens.activeFlows.purgedelay',{delay:flow.options.purgeDelay}) : ''}}</td>
                    <td>{{ flow.options.errorOnActivityTimeout ? translate('common.fail') : translate('common.continue') }}</td>
                    <td>{{ flow.options.errorOnActivityFailure ? translate('common.fail') : translate('common.continue') }}</td>
                    <td>{{ shrinkString(JSON.stringify(flow.arguments, null, 2), 100) }}</td>
                    <td>{{ shrinkString(JSON.stringify(flow.metaData, null, 2), 100) }}</td>
                    <td>{{ flow.steps.length }}</td>
                    <td>{{ getCurrentStep(flow.steps) }}</td>
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
    import {GetActiveFlows} from 'services';
    import {Constants, shrinkString} from 'mixins';
    import {ref, inject} from 'vue';
    import {Button, Sizes} from 'components';
    import {translate, formatTime} from '../i18n';

    const currentNamespace = inject(Constants.namespaceName);

    const flows = ref([]);
    const lastOffset = ref(0);
    const pageSize = 10;
    const metadata = null; // Replace with actual metadata if needed
    const argument = null; // Replace with actual argument if needed
    const currentWorkflow = ref(null);

    const loadMore = async () => {
        const newFlows = await GetActiveFlows(currentNamespace.value, "Place Order", metadata, argument, lastOffset.value, pageSize);
        flows.value.push(...newFlows.data);
        lastOffset.value = newFlows.lastOffset;
        return newFlows.data.length === pageSize; // Return true if there might be more data to load
    };

    const getCurrentStep = (steps) => {
        const currentStep = steps[steps.length-1];
        switch(currentStep.type){
            case 'Action':
                return currentStep.name;
            case 'Delay':
                return translate('screens.activeFlows.delayed',{
                    starttime:formatTime(currentStep.startTime),
                    endtime:(currentStep.input ? translate('screens.activeFlows.delayedfor',{time:currentStep.input}) : '')
                });
            case 'Suspended':
                return translate('screens.activeFlows.suspended',{starttime:formatTime(currentStep.startTime)});
        }
    };
    
</script>