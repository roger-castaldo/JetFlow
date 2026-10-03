<template>
    <Table :fixed_header="props.fixed_header" :full_width="props.full_width">
        <template v-if="slots.thead" #thead>
            <slot name="thead"/>
        </template>
        <template v-if="slots.tbody" #tbody>
            <slot name="tbody"/>
        </template>
        <template #tfoot>
            <slot name="tfoot"/>
            <!-- Sentinel -->
            <tr ref="loadMoreTrigger">
                <td colspan="100%">
                    {{ loading ? 'Loading...' : (hasMore ? '' : 'No more results') }}
                </td>
            </tr>
        </template>
    </Table>
</template>

<script setup>
    import {Table} from "components";
    import {useSlots, onMounted, onUnmounted, ref} from "vue";

    const hasMore = ref(true);
    const loading = ref(false);
    const loadMoreTrigger = ref(null)

    const slots = useSlots();
    
    const props = defineProps({
        fixed_header:{
            type:Boolean,
            default:false
        },
        full_width:{
            type:Boolean,
            default:false
        },
        loadMore:{
            type:Function,
            required:true
        }
    });

    let observer = null;

    onMounted(async () => {
        observer = new IntersectionObserver(
            async (entries) => {
                if (entries[0].isIntersecting) {
                    loading.value = true;
                    hasMore.value = await props.loadMore();
                    loading.value = false;
                }
            },
            {
                // Start loading before the user actually reaches the bottom.
                rootMargin: '300px'
            }
        )

        observer.observe(loadMoreTrigger.value)

        // // Initial page
        // hasMore.value = await props.loadMore();
    });

    onUnmounted(() => {
        observer?.disconnect()
    });

</script>