// Stand-alone entry for the print preview (src/print/preview.html). It is served
// by the dev server only and is not part of the application build, so the mock
// documents can never ship. Open http://localhost:5173/src/print/preview.html
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '../styles.css'
import './preview.css'
import { PrintPreviewPage } from './PrintPreviewPage'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <PrintPreviewPage />
  </StrictMode>,
)
