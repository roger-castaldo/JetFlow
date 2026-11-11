<template>
    <Slideout :show="props.show" :has_close="true" @close="emit('close')">
        <Table :fixed_header="true" v-show="chartedItem===null">
            <template #thead>
                <tr>
                    <th class="has-text-centered" colspan="100%">
                        {{ translate(`components.performancedetails.${props.forWorkflow ? 'workflowperformance' : 'activityperformance'}`) }}
                    </th>
                </tr>
                <tr>
                    <th rowspan="2">{{ translate('common.name') }}</th>
                    <th rowspan="2">{{ translate('components.performancedetails.started') }}</th>
                    <th rowspan="2">{{ translate('components.performancedetails.completed') }}</th>
                    <th rowspan="2">{{ translate('components.performancedetails.failed') }}</th>
                    <th rowspan="2" >{{ props.forWorkflow ? translate('components.performancedetails.purged') : translate('components.performancedetails.timedout')  }}</th>
                    <th colspan="3">{{ translate('components.performancedetails.queuelatencies') }}</th>
                    <th v-if="!props.forWorkflow" colspan="3">{{ translate('components.performancedetails.durations') }}</th>
                    <th rowspan="2">
                        <ButtonRefresh @click="reloadPerformanceData" />
                    </th>
                </tr>
                <tr>
                    <th>P50</th>
                    <th>P95</th>
                    <th>P99</th>
                    <th v-if="!props.forWorkflow">P50</th>
                    <th v-if="!props.forWorkflow">P95</th>
                    <th v-if="!props.forWorkflow">P99</th>
                </tr>
            </template>
            <template #tbody>
                <tr v-for="(item, index) in performanceData" :key="index">
                    <td>{{ item.name }}</td>
                    <td>{{ item.started }}</td>
                    <td>{{ item.completed }}</td>
                    <td>{{ item.failed }}</td>
                    <td>{{ props.forWorkflow ? item.purged : item.timedOut }}</td>
                    <td>{{ formatTimeSpan(item.queueLatency.p50) }}</td>
                    <td>{{ formatTimeSpan(item.queueLatency.p95) }}</td>
                    <td>{{ formatTimeSpan(item.queueLatency.p99) }}</td>
                    <td v-if="!props.forWorkflow">{{ formatTimeSpan(item.duration.p50) }}</td>
                    <td v-if="!props.forWorkflow">{{ formatTimeSpan(item.duration.p95) }}</td>
                    <td v-if="!props.forWorkflow">{{ formatTimeSpan(item.duration.p99) }}</td>
                    <td>
                        <Icon icon="magnifying-glass"  class="is-clickable" @click="chartElement(item.name)"/>
                    </td>
                </tr>
            </template>
        </Table>
        <section v-show="chartedItem!==null" style="min-width:500px">
            <Chart v-bind="charts.QueueTiming" :show="chartedItem!==null" />
            <Chart v-if="!props.forWorkflow" v-bind="charts.PerformanceTiming" :show="chartedItem!==null" />
            <Chart v-bind="charts.Numbers" :show="chartedItem!==null" />
        </section>
    </Slideout>
</template>

<script setup>
    import {Slideout, Table, ButtonRefresh, Chart, Icon, ChartTypes} from 'components';
    import {Constants, formatTimeSpan, extractNumericalValueFromTimeSpan} from "mixins";
    import {translate, formatTime} from '../i18n';
    import {onMounted, ref, inject, computed} from 'vue';
    import {GetLatestActivityPerformance, GetLatestWorkflowPerformance, GetActivityPerformance, GetWorkflowPerformance} from 'services';

    const props = defineProps({
        show: {
            type: Boolean,
            default: false
        },
        forWorkflow: {
            type: Boolean,
            default: true
        }
    });

    const emit = defineEmits(['close']);

    const currentNamespace = inject(Constants.namespaceName);

    const performanceData = ref([]);
    const chartedItem = ref(null);

    const reloadPerformanceData = async () => {
        performanceData.value = [];
        if (props.forWorkflow) {
            performanceData.value = await GetLatestWorkflowPerformance(currentNamespace.value);
        } else {
            performanceData.value = await GetLatestActivityPerformance(currentNamespace.value);
        }
    }

    onMounted(async () => {
        chartedItem.value = null;
        await reloadPerformanceData();
    });

    const chartElement = (name) => {
        chartedItem.value = {
            name: name,
            data: [],
            dateRange: {
                start: new Date(new Date().getTime() - (5 * 60 * 1000)),
                end: new Date()
            }
        };
        reloadChartData();
    }

    const reloadChartData = async () => {
        if (chartedItem.value === null) return;
        let data = [];
        if (props.forWorkflow) {
            data = await GetWorkflowPerformance(currentNamespace.value, chartedItem.value.name, chartedItem.value.dateRange.start, chartedItem.value.dateRange.end);
        } else {
            data = await GetActivityPerformance(currentNamespace.value, chartedItem.value.name, chartedItem.value.dateRange.start, chartedItem.value.dateRange.end);
        }
        chartedItem.value.data = data;
    }

    const charts = computed(() => {
        return {
            QueueTiming:{
                type: ChartTypes.Line,
                title: translate('components.performancedetails.queuelatencies'),
                labels: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>formatTime(d.sampleWindow))),
                datasets: [
                    {
                        label: 'P50',
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.queueLatency.p50))),
                    },
                    {
                        label: 'P95',
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.queueLatency.p95))),
                    },
                    {
                        label: 'P99',
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.queueLatency.p99))),
                    }
                ],
                scales: {
                    y:{
                        ticks:{
                            callback: (value) => {
                                return formatTimeSpan(value);
                            }
                        }
                    }
                }
            },
            PerformanceTiming: (
                props.forWorkflow ? null : {
                    type: ChartTypes.Line,
                    title: translate('components.performancedetails.durations'),
                    labels: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>formatTime(d.sampleWindow))),
                    datasets: [
                        {
                            label: 'P50',
                            data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.duration.p50))),
                        },
                        {
                            label: 'P95',
                            data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.duration.p95))),
                        },
                        {
                            label: 'P99',
                            data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>extractNumericalValueFromTimeSpan(d.duration.p99))),
                        }
                    ],
                    scales: {
                        y:{
                            ticks:{
                                callback: (value, index, ticks) => {
                                    return formatTimeSpan(value);
                                }
                            }
                        }
                    }
                }
            ),
            Numbers:{
                type: ChartTypes.Line,
                title: translate('components.performancedetails.counts'),
                labels: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>formatTime(d.sampleWindow))),
                datasets:[
                    {
                        label: translate('components.performancedetails.started'),
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>d.started)),
                    },
                    {
                        label: translate('components.performancedetails.completed'),
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>d.completed)),
                    },
                    {
                        label: translate('components.performancedetails.failed'),
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>d.failed)),
                    },
                    {
                        label: props.forWorkflow ? translate('components.performancedetails.purged') : translate('components.performancedetails.timedout'),
                        data: (chartedItem.value===null ? [] : chartedItem.value.data.map(d=>props.forWorkflow ? d.purged : d.timedOut)),
                    }
                ]
            }
        };
    });
</script>