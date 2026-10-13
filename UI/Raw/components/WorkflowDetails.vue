<template>
    <Slideout :show="props.workflow!==null" :has_close="true" @close="emit('close')">
        <template #header>
            {{ translate('components.workflowdetails.title',{name:props.workflow?.name}) }}
        </template>
        <template #content>
            <div v-if="props.workflow!==null">
                <p>
                    <strong>{{ translate('common.workflowid') }}:</strong> 
                    {{ props.workflow.id }}
                </p>
                <p v-if="props.workflow.schedulerId!==null">
                    <strong>{{ translate('common.schedulerid') }}:</strong> 
                    {{ props.workflow.schedulerId }}
                </p>
                <p>
                    <strong>{{ translate('common.startedat') }}:</strong> 
                    {{ formatTime(props.workflow.startedAt) }}
                </p>
                <p v-if="props.workflow.finishedAt">
                    <strong>{{ translate('common.completedat') }}:</strong> 
                    {{ formatTime(props.workflow.finishedAt) }}
                </p>
                <p>
                    <strong>{{ translate('common.oncomplete') }}:</strong> 
                    {{ props.workflow.options.completionAction }}{{ props.workflow.options.purgeDelay!==null ? translate('components.workflowdetails.purgedelay',{delay:props.workflow.options.purgeDelay}) : ''}}
                </p>
                <p>
                    <strong>{{ translate('common.onactivitytimeout') }}:</strong> 
                    {{ props.workflow.options.errorOnActivityTimeout ? translate('common.fail') : translate('common.continue') }}
                </p>
                <p>
                    <strong>{{ translate('common.onactivityerror') }}:</strong> 
                    {{ props.workflow.options.errorOnActivityFailure ? translate('common.fail') : translate('common.continue') }}
                </p>
                <p v-if="props.workflow.finishedAt">
                    <strong>{{ translate('components.workflowdetails.successful') }}:</strong> 
                    {{ props.workflow.isSuccessful ? translate('common.yes') : translate('common.no') }}
                </p>
                <p v-else>
                    <strong>{{ translate('components.workflowdetails.status') }}:</strong> 
                    {{ translate('components.workflowdetails.inprogress') }}
                </p>
                <p v-if="props.workflow.isSuccessful===false">
                    <strong>{{ translate('components.workflowdetails.failurereason') }}:</strong> 
                    {{ props.workflow.errorMessage }}
                </p>
                <p v-if="props.workflow.arguments!==null">
                    <strong>{{ translate('common.argument') }}:</strong>
                    <pre class="box">{{ JSON.stringify(props.workflow.arguments, null, 2) }}</pre>
                </p>
                <p v-if="props.workflow.metaData!==null">
                    <strong>{{ translate('common.metadata') }}:</strong>
                    <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                        <li v-for="pair in props.workflow.metaData" :key="pair.key">
                            <strong>{{ pair.key }}:</strong> 
                            {{ pair.value.join(', ') }}
                        </li>
                    </List>
                </p>
                <p>
                    <strong>{{ translate('components.workflowdetails.steps') }}:</strong>
                </p>
                <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                    <li v-for="(step,index) in props.workflow.steps" :key="index">
                        <template v-if="step.type === 'Delay'">
                            {{ translate('components.workflowdetails.delayed',{
                                startime:formatTime(step.startTime),
                                endtime:(step.endTime===null ? 
                                    translate('components.workflowdetails.delayedfor',{time:step.input}) :
                                    translate('components.workflowdetails.delayeduntil',{time:formatTime(step.endtime)})
                                )
                            }) }}
                        </template>
                        <template v-if="step.type === 'Suspended'">
                            {{ translate('components.workflowdetails.suspended',{
                                starttime:formatTime(step.startTime),
                                endtime: (step.endTime === null ? 
                                    '' :
                                    translate('components.workflowdetails.suspendeduntil', {time:formatTime(step.endTime)})
                                ),
                                reason:(step.input===null ? '' : `(${step.input})`)
                            }) }}
                        </template>
                        <template v-if="step.type === 'Action'">    
                            <p>
                                <strong>{{ step.name }}</strong> 
                                @{{ formatTime(step.startTime) }}
                            </p>
                            <p v-if="step.input!==null">
                                <strong>{{ translate('components.workflowdetails.input') }}:</strong> 
                                <pre class="box">{{ JSON.stringify(step.input, null, 2) }}</pre>
                            </p>
                            <p v-if="step.retries!==null && step.retries.length>0">
                                <strong>{{ translate('components.workflowdetails.retries') }}:</strong>
                                <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                                    <li v-for="(retry,index) in step.retries" :key="index">
                                        {{ retry.retryType }} @{{ formatTime(retry.timestamp) }}
                                    </li>
                                </List>
                            </p>
                            <template v-if="step.endTime!==null">
                                <p>
                                    <strong>{{ translate('components.workflowdetails.finishedat') }}:</strong>
                                    @{{ formatTime(step.endTime) }}
                                </p>
                                <p>
                                    <strong>{{ translate('components.workflowdetails.resultstatus') }}:</strong> 
                                    {{ step.status }}
                                </p>
                                <p v-if="step.errorMessage!==null">
                                    <strong>{{ translate('components.workflowdetails.errormessage') }}:</strong> 
                                    {{ step.errorMessage }}
                                </p>
                                <p v-if="step.result!==null">
                                    <strong>{{ translate('components.workflowdetails.result') }}:</strong> 
                                    <pre class="box">{{ JSON.stringify(step.result, null, 2) }}</pre>
                                </p>
                            </template>
                        </template>
                    </li>
                </List>
            </div>
            <div v-else>
                <p>{{ translate('components.workflowdetails.nodetails') }}</p>
            </div>
        </template>
    </Slideout>
</template>

<script setup>
    import {Slideout, List, ColorTypes} from 'components';
    import {Constants} from 'mixins';
    import {translate, formatTime} from '../i18n';

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