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
                <Table :narrow="true" :fixed_header="true">
                    <template #thead>
                        <tr>
                            <th colspan="100%"><strong>{{ translate('components.workflowdetails.steps') }}</strong></th>
                        </tr>
                        <tr>
                            <th>{{ translate('components.workflowdetails.type') }}</th>
                            <th>{{ translate('components.workflowdetails.starttime') }}</th>
                            <th>{{ translate('components.workflowdetails.endtime') }}</th>
                            <th>{{ translate('components.workflowdetails.name') }}</th>
                            <th>{{ translate('components.workflowdetails.input') }}</th>
                            <th>{{ translate('components.workflowdetails.retries') }}</th>
                            <th>{{ translate('components.workflowdetails.resultstatus') }}</th>
                            <th>{{ translate('components.workflowdetails.result') }}/{{ translate('components.workflowdetails.errormessage') }}</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="(step,index) in props.workflow.steps" :key="index">
                            <td>
                                {{ (step.type==='Delay' ? translate('components.workflowdetails.delayed') : null) }}
                                {{ (step.type==='Suspended' ? translate('components.workflowdetails.suspended') : null) }}
                                {{ (step.type==='Action' ? translate('components.workflowdetails.action') : null) }}
                            </td>
                            <td>
                                {{ formatTime(step.startTime) }}
                            </td>
                            <td>
                                {{ (step.endTime===null && step.type==='Delayed' ? translate('components.workflowdetails.for',{time:step.input}) : null) }}
                                {{ (step.endTime!==null ? formatTime(step.endTime) : null) }}
                            </td>
                            <td>
                                {{ step.name }}
                            </td>
                            <td>
                                {{ step.input!==null && step.type==='Suspended' ? step.input : null }}
                                <pre class="box" v-if="step.input!==null && step.type==='Action'">{{ JSON.stringify(step.input, null, 2) }}</pre>
                            </td>
                            <td>
                                <List :compact="true" :highlighted="true" :type="ColorTypes.info">
                                    <li v-for="(retry,index) in step.retries" :key="index">
                                        {{ retry.retryType }} @{{ formatTime(retry.timestamp) }}
                                    </li>
                                </List>
                            </td>
                            <td>
                                {{ step.status }}
                            </td>
                            <td>
                                {{ step.errorMessage }}
                                <pre class="box" v-if="step.result!==null">{{ JSON.stringify(step.result, null, 2) }}</pre>
                            </td>
                        </tr>
                    </template>
                </Table>
            </div>
            <div v-else>
                <p>{{ translate('components.workflowdetails.nodetails') }}</p>
            </div>
        </template>
    </Slideout>
</template>

<script setup>
    import {Slideout, List, ColorTypes, Table} from 'components';
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