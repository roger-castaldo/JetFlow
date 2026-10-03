<template>
    <div>
        <InfiniteTable :fixed_header="true" :full_width="true" :loadMore="loadMore">
            <template #thead>
                <tr>
                    <th>Scheduler ID</th>
                    <th>Started At</th>
                    <th>On Complete</th>
                    <th>On Activity Timeout</th>
                    <th>On Activity Failure</th>
                    <th>Argument</th>
                    <th>MetaData</th>
                    <th>StepCount</th>
                    <th>Current Step</th>
                    <th></th>
                </tr>
            </template>
            <template #tbody>
                <tr v-for="flow in flows" :key="flow.ID">
                    <td>{{ flow.schedulerId }}</td>
                    <td>{{ Constants.timeFormatter.format(new Date(flow.startedAt)) }}</td>
                    <td>{{ flow.options.completionAction }}{{ flow.options.purgeDelay!==null ? ` (Purge Delay: ${flow.options.purgeDelay})` : ''}}</td>
                    <td>{{ flow.options.errorOnActivityTimeout ? 'Fail' : 'Continue' }}</td>
                    <td>{{ flow.options.errorOnActivityFailure ? 'Fail' : 'Continue' }}</td>
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
    import {Constants} from 'mixins';
    import {ref, inject} from 'vue';
    import {Button, Sizes} from 'components';

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
                return `Delayed @${Constants.timeFormatter.format(new Date(currentStep.startTime))} ${(currentStep.input ? ` for ${Constants.timeFormatter.format(new Date(currentStep.input))}` : '')}`;
            case 'Suspended':
                return `Suspended @${Constants.timeFormatter.format(new Date(currentStep.startTime))}`;
        }
    };

    const shrinkString = (str, maxLength) => {
        if (str.length > maxLength) {
            return str.substring(0, maxLength) + '...';
        }
        return str;
    };
    
</script>