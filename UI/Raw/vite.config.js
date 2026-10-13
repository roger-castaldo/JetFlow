import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
    plugins: [vue()],

    build: {
        sourcemap:true, 
        outDir: '../Core/wwwroot/jetflow/resources',
        emptyOutDir: false,

        lib: {
            entry: 'index.ts',
            formats: ['es'],
            fileName: () => 'app.js',
            cssFileName: 'app'
        },

        rollupOptions: {
            external: ['vue', 'components', 'mixins', 'services']
        },

        minify: 'terser'
    },
    define: {
        'process.env.NODE_ENV': JSON.stringify('production')
    }
})