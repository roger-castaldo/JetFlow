<template>
    <Slideout :show="props.workflow!==null" :has_close="true" @close="emit('close')">
        <template #header>
            Workflow Details {{ props.workflow?.name }}
        </template>
        <template #content>
            <div v-if="props.workflow!==null">
                <p><strong>Workflow ID:</strong> {{ props.workflow.id }}</p>
                <p v-if="props.workflow.schedulerId!==null"><strong>Scheduler ID:</strong> {{ props.workflow.schedulerId }}</p>
                <p><strong>Started At:</strong> {{ Constants.timeFormatter.format(new Date(props.workflow.startedAt)) }}</p>
                <p v-if="props.workflow.finishedAt"><strong>Completed At:</strong> {{ Constants.timeFormatter.format(new Date(props.workflow.finishedAt)) }}</p>
                <p><strong>On Complete:</strong> {{ props.workflow.options.completionAction }}{{ props.workflow.options.purgeDelay!==null ? ` (Purge Delay: ${props.workflow.options.purgeDelay})` : ''}}</p>
                <p><strong>On Activity Timeout:</strong> {{ props.workflow.options.errorOnActivityTimeout ? 'Fail' : 'Continue' }}</p>
                <p><strong>On Activity Error:</strong> {{ props.workflow.options.errorOnActivityFailure ? 'Fail' : 'Continue' }}</p>
                <p v-if="props.workflow.finishedAt"><strong>Successful:</strong> {{ props.workflow.isSuccessful ? 'Yes' : 'No' }}</p>
                <p v-else><strong>Status:</strong> In Progress</p>
                <p v-if="props.workflow.isSuccessful===false"><strong>Failure Reason:</strong> {{ props.workflow.errorMessage }}</p>
                <p v-if="props.workflow.arguments!==null">
                    <strong>Arguments:</strong>
                    <pre class="box">{{ JSON.stringify(props.workflow.arguments, null, 2) }}</pre>
                </p>
                <p v-if="props.workflow.metaData!==null">
                    <strong>MetaData:</strong>
                    <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                        <li v-for="pair in props.workflow.metaData" :key="pair.key">
                            <strong>{{ pair.key }}:</strong> {{ pair.value.join(', ') }}
                        </li>
                    </List>
                </p>
                <p><strong>Steps:</strong></p>
                <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                    <li v-for="(step,index) in props.workflow.steps" :key="index">
                        <template v-if="step.type === 'Delay'">
                            Delayed @{{ Constants.timeFormatter.format(new Date(step.startTime)) }} {{ step.endTime===null ? `for ${step.input}`  : `until ${Constants.timeFormatter.format(new Date(step.endTime))}` }}
                        </template>
                        <template v-if="step.type === 'Suspended'">
                            Suspended @{{ Constants.timeFormatter.format(new Date(step.startTime)) }} {{ step.endTime!==null ? `until ${Constants.timeFormatter.format(new Date(step.endTime))}` : ''}} {{ step.input!==null ? `(${step.input})` : '' }}
                        </template>
                        <template v-if="step.type === 'Action'">    
                            <p><strong>{{ step.name }}</strong> @{{ Constants.timeFormatter.format(new Date(step.startTime)) }}</p>
                            <p v-if="step.input!==null">
                                <strong>Input:</strong> 
                                <pre class="box">{{ JSON.stringify(step.input, null, 2) }}</pre>
                            </p>
                            <p v-if="step.retries!==null && step.retries.length>0">
                                <strong>Retries:</strong>
                                <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                                    <li v-for="(retry,index) in step.retries" :key="index">
                                        {{ retry.retryType }} @{{ Constants.timeFormatter.format(new Date(retry.timestamp)) }}
                                    </li>
                                </List>
                            </p>
                            <template v-if="step.endTime!==null">
                                <p><strong>Finished At:</strong>@{{ Constants.timeFormatter.format(new Date(step.endTime)) }}</p>
                                <p><strong>Result Status:</strong> {{ step.status }}</p>
                                <p v-if="step.errorMessage!==null"><strong>Error Message:</strong> {{ step.errorMessage }}</p>
                                <p v-if="step.result!==null">
                                    <strong>Result:</strong> 
                                    <pre class="box">{{ JSON.stringify(step.result, null, 2) }}</pre>
                                </p>
                            </template>
                        </template>
                    </li>
                </List>
            </div>
            <div v-else>
                <p>No workflow details available.</p>
            </div>
        </template>
    </Slideout>
</template>

<script setup>
    import {Slideout, List, ColorTypes} from 'components';
    import {Constants} from 'mixins';

    const props = defineProps({
        workflow: {
            type: Object,
            default: null
        }
    });

    const emit = defineEmits(['close']);
</script>

<style>
.slideout-content .slideout-head{
    top:-20px;
}

pre.box{
    margin:3px;
}
</style>