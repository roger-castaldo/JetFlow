<template>
    <section>
        <ColumnContainer :modifiers="[ColumnContainerModifiers.fullWidth]"
                 :columns="[{name:'performanceCounters',size:ColumnSizes.two},{name:'workflowServiceability',size:ColumnSizes.five},{name:'activityServiceability',size:ColumnSizes.five}]">
            <template #performanceCounters>
                <Card>
                    <template #header="props">
                        <h1 :class="props.header_class">Active Counters</h1>
                    </template>
                    <template #content>
                        {{ translate('screens.dashboard.activeflows') }}: {{performanceCounters.activeWorkflows}}<br/>
                        {{ translate('screens.dashboard.suspendedflows') }}: {{performanceCounters.suspendedWorkflows}}<br/>
                        {{ translate('screens.dashboard.activeactivities') }}: {{performanceCounters.activeActivities}}
                    </template>
                </Card>
            </template>
            <template #workflowServiceability>
                <Table>
                    <template #thead>
                        <tr>
                            <th colspan="100%" class="has-text-centered">{{ translate('screens.dashboard.workflowserviceability') }}</th>
                        </tr>
                        <tr>
                            <th>{{ translate('common.name') }}</th>
                            <th>{{ translate('screens.dashboard.idleinstances') }}</th>
                            <th>{{ translate('screens.dashboard.activeinstances') }}</th>
                            <th>{{ translate('screens.dashboard.messageswaiting') }}</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="serv in workflowServiceability">
                            <td>{{ serv.name }}</td>
                            <td>{{ serv.idleInstances }}</td>
                            <td>{{ serv.activeInstances }}</td>
                            <td>{{ serv.messagesWaiting }}</td>
                        </tr>
                    </template>
                </Table>
            </template>
            <template #activityServiceability>
                <Table>
                    <template #thead>
                        <tr>
                            <th colspan="100%" class="has-text-centered">{{ translate('screens.dashboard.activityserviceability') }}</th>
                        </tr>
                        <tr>
                            <th>{{ translate('common.name') }}</th>
                            <th>{{ translate('screens.dashboard.idleinstances') }}</th>
                            <th>{{ translate('screens.dashboard.activeinstances') }}</th>
                            <th>{{ translate('screens.dashboard.messageswaiting') }}</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="serv in activityServiceability">
                            <td>{{ serv.name }}</td>
                            <td>{{ serv.idleInstances }}</td>
                            <td>{{ serv.activeInstances }}</td>
                            <td>{{ serv.messagesWaiting }}</td>
                        </tr>
                    </template>
                </Table>
            </template>
        </ColumnContainer>
        <ColumnContainer :modifiers="[ColumnContainerModifiers.fullWidth]"
                 :columns="[{name:'activityPerformance',size:ColumnSizes.six},{name:'workflowPerformance',size:ColumnSizes.six}]">
            <template #activityPerformance>
                <Table>
                    <template #thead>
                        <tr>
                            <th colspan="100%" class="has-text-centered">{{ translate('screens.dashboard.activityperformance') }}</th>
                        </tr>
                        <tr>
                            <th>{{ translate('screens.dashboard.time') }}</th>
                            <th>{{ translate('common.name') }}</th>
                            <th>{{ translate('screens.dashboard.started') }}</th>
                            <th>{{ translate('screens.dashboard.completed') }}</th>
                            <th>{{ translate('screens.dashboard.failed') }}</th>
                            <th>{{ translate('screens.dashboard.timedout') }}</th>
                            <th>{{ translate('screens.dashboard.avgqueuelatency') }}</th>
                            <th>{{ translate('screens.dashboard.avgduration') }}</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in activityPerformance">
                            <td>{{ formatTime(perf.window) }}</td>
                            <td>{{ perf.name }}</td>
                            <td>{{ perf.started }}</td>
                            <td>{{ perf.completed }}</td>
                            <td>{{ perf.failed }}</td>
                            <td>{{ perf.timedOut }}</td>
                            <td>{{ perf.averageQueueLatencies }}</td>
                            <td>{{ perf.averageDurations }}</td>
                        </tr>
                    </template>
                </Table>
            </template>
            <template #workflowPerformance>
                <Table>
                    <template #thead>
                        <tr>
                            <th colspan="100%" class="has-text-centered">{{ translate('screens.dashboard.workflowperformance') }}</th>
                        </tr>
                        <tr>
                            <th>{{ translate('screens.dashboard.time') }}</th>
                            <th>{{ translate('common.name') }}</th>
                            <th>{{ translate('screens.dashboard.started') }}</th>
                            <th>{{ translate('screens.dashboard.completed') }}</th>
                            <th>{{ translate('screens.dashboard.failed') }}</th>
                            <th>{{ translate('screens.dashboard.purged') }}</th>
                            <th>{{ translate('screens.dashboard.avgqueuelatency') }}</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in workflowPerformance">
                            <td>{{ formatTime(perf.window) }}</td>
                            <td>{{ perf.name }}</td>
                            <td>{{ perf.started }}</td>
                            <td>{{ perf.completed }}</td>
                            <td>{{ perf.failed }}</td>
                            <td>{{ perf.purged }}</td>
                            <td>{{ perf.averageQueueLatencies }}</td>
                        </tr>
                    </template>
                </Table>
            </template>
        </ColumnContainer>
    </section>
</template>

<script setup>
    import {GetDashboardStream} from "services";
    import {ref, onUnmounted, inject, watch} from "vue";
    import {ColumnContainerModifiers, ColumnContainer, ColumnSizes, Card, Table} from "components";
    import {Constants} from "mixins";
    import {translate, formatTime} from '../i18n';

    const performanceCounters = ref({
        activeWorkflows:0,
        suspendedWorkflows:0,
        activeActivities:0
    });
    const activityPerformance = ref([]);
    const workflowPerformance = ref([]);
    const workflowServiceability = ref([]);
    const activityServiceability = ref([]);
    const currentNamespace = inject(Constants.namespaceName);

    const establishDashboard = () =>
    {
        let result = GetDashboardStream(currentNamespace.value);
        result.addEventListener("performanceCounters", (e) => {
            performanceCounters.value = JSON.parse(e.data);
        });
        result.addEventListener("activityPerformance", (e)=>{
            activityPerformance.value = JSON.parse(e.data);
        });
        result.addEventListener("workflowPerformance", (e)=>{
            workflowPerformance.value = JSON.parse(e.data);
        });
        result.addEventListener("workflowServiceability", (e)=>{
            workflowServiceability.value = JSON.parse(e.data);
        });
        result.addEventListener("activityServiceability", (e)=>{
            activityServiceability.value = JSON.parse(e.data);
        });
        return result;
    };

    let dashboardStream = establishDashboard();

    watch(currentNamespace,()=>{
        dashboardStream.close();
        dashboardStream = establishDashboard();
    });

    onUnmounted(()=>dashboardStream.close());
</script>