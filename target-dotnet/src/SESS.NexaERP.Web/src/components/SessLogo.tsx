/** SESS badge used as the brand mark in the sidebar and on the login card. */
export function SessLogo({ className = 'brand-mark' }: { className?: string }) {
  return (
    <span className={className} aria-hidden="true">
      <svg viewBox="0 0 40 40" role="presentation" focusable="false">
        <text
          x="20"
          y="21"
          textAnchor="middle"
          dominantBaseline="central"
          fontFamily="Inter, 'Segoe UI', system-ui, sans-serif"
          fontSize="13"
          fontWeight="700"
          letterSpacing="0.5"
          fill="currentColor"
        >
          SESS
        </text>
      </svg>
    </span>
  )
}
