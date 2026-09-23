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
                        Active Workflows: {{performanceCounters.activeWorkflows}}<br/>
                        Suspended Workflows: {{performanceCounters.suspendedWorkflows}}<br/>
                        Active Activities: {{performanceCounters.activeActivities}}
                    </template>
                </Card>
            </template>
            <template #workflowServiceability>
                <Table>
                    <template #thead>
                        <tr>
                            <th colspan="100%" class="has-text-centered">Workflow Serviceability</th>
                        </tr>
                        <tr>
                            <th>Name</th>
                            <th>Idle Instances</th>
                            <th>Active Instances</th>
                            <th>Messages Waiting</th>
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
                            <th colspan="100%" class="has-text-centered">Activity Serviceability</th>
                        </tr>
                        <tr>
                            <th>Name</th>
                            <th>Idle Instances</th>
                            <th>Active Instances</th>
                            <th>Messages Waiting</th>
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
                            <th colspan="100%" class="has-text-centered">Activity Performance</th>
                        </tr>
                        <tr>
                            <th>Time</th>
                            <th>Name</th>
                            <th>Started</th>
                            <th>Completed</th>
                            <th>Failed</th>
                            <th>TimedOut</th>
                            <th>Avg. Queue Latencies</th>
                            <th>Avg. Durations</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in activityPerformance">
                            <td>{{ timeFormatter.format(new Date(perf.window)) }}</td>
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
                            <th colspan="100%" class="has-text-centered">Workflow Performance</th>
                        </tr>
                        <tr>
                            <th>Time</th>
                            <th>Name</th>
                            <th>Started</th>
                            <th>Completed</th>
                            <th>Failed</th>
                            <th>Purged</th>
                            <th>Avg. Queue Latencies</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in workflowPerformance">
                            <td>{{ timeFormatter.format(new Date(perf.window)) }}</td>
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

    const timeFormatter = new Intl.DateTimeFormat('en-US', { dateStyle: 'short', timeStyle: 'short' });

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