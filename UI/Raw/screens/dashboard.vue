<template>
    <section>
        <ColumnContainer :modifiers="[ColumnContainerModifiers.fullWidth]"
                 :columns="[{name:'performanceCounters',size:ColumnSizes.three}]">
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
                            <th>Queue Latencies</th>
                            <th>Durations</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in activityPerformance">
                            <td>{{ perf.window }}</td>
                            <td>{{ perf.Name }}</td>
                            <td>{{ perf.Started }}</td>
                            <td>{{ perf.Completed }}</td>
                            <td>{{ perf.Failed }}</td>
                            <td>{{ perf.TimedOut }}</td>
                            <td>{{ perf.queueLatencies.slice(0, 10).join(', ') }}</td>
                            <td>{{ perf.durations.slice(0, 10).join(', ') }}</td>
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
                            <th>Queue Latencies</th>
                        </tr>
                    </template>
                    <template #tbody>
                        <tr v-for="perf in workflowPerformance">
                            <td>{{ perf.window }}</td>
                            <td>{{ perf.Name }}</td>
                            <td>{{ perf.Started }}</td>
                            <td>{{ perf.Completed }}</td>
                            <td>{{ perf.Failed }}</td>
                            <td>{{ perf.Purged }}</td>
                            <td>{{ perf.queueLatencies.slice(0, 10).join(', ') }}</td>
                        </tr>
                    </template>
                </Table>
            </template>
        </ColumnContainer>
    </section>
</template>

<script setup>
    import {GetDashboardStream} from "services";
    import {ref, onUnmounted} from "vue";
    import {ColumnContainerModifiers, ColumnContainer, ColumnSizes, Card, Table} from "components";

    const performanceCounters = ref({
        activeWorkflows:0,
        suspendedWorkflows:0,
        activeActivities:0
    });
    const activityPerformance = ref([]);
    const workflowPerformance = ref([]);

    const dashboardStream = GetDashboardStream();

    dashboardStream.addEventListener("performanceCounters", (e) => {
        performanceCounters.value = JSON.parse(e.data);
    });

    dashboardStream.addEventListener("activityPerformance", (e)=>{
        activityPerformance.value = JSON.parse(e.data);
    });

    dashboardStream.addEventListener("workflowPerformance", (e)=>{
        workflowPerformance.value = JSON.parse(e.data);
    });

    onUnmounted(()=>dashboardStream.close());
</script>