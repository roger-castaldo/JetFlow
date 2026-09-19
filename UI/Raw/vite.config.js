import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
    plugins: [vue()],

    build: {
        outDir: '../Core/wwwroot/jetflow/resources',
        emptyOutDir: false,

        lib: {
            entry: 'app.vue',
            formats: ['es'],
            fileName: () => 'app.js',
            cssFileName: 'app'
        },

        rollupOptions: {
            external: ['vue', 'components', 'mixins', 'services']
        },

        minify: 'terser'
    }
})