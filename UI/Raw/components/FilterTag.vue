<template>
    <Tag :rounded="true" :type="ColorTypes.info">
        <slot name="default"/>
        <button class="delete is-small" @click="emit('delete')"/>
    </Tag>
</template>
<script setup>
    import {Tag, ColorTypes} from 'components';
    const emit = defineEmits(['delete']);
</script>