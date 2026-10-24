import { SCENES } from "../storyboard";
import styles from "./Scenes.module.css";

export function ProblemScene() {
  const scene = SCENES[0];
  return <section data-promo-scene={scene.id} className={`${styles.scene} ${styles.dark}`} aria-label={scene.headline}>
    <div className={styles.topline}><span>GF3</span><span>Każdy dzień. Wiele ustaleń.</span></div>
    <div className={styles.heading}><h1 data-promo-headline>{scene.headline}</h1><p data-promo-supporting>{scene.supporting}</p></div>
    <div className={styles.disorder}>
      <div className={`${styles.looseCard} ${styles.looseGrid}`} data-promo-chaos="grid"><small>GRAFIK ZESPOŁU</small><strong>Pon · Wt · Śr</strong><div className={styles.looseCells}><span>09–17</span><span>12–20</span><span>?</span><span>12–20</span><span>?</span><span>09–17</span></div></div>
      <div className={`${styles.looseCard} ${styles.looseMessages}`} data-promo-chaos="messages"><small>WIADOMOŚCI</small><strong>Kto może się zamienić?</strong><p>Zmieniły mi się plany…</p><span className={styles.message}>Jaki mam grafik na środę?</span></div>
      <div className={`${styles.looseCard} ${styles.looseSlip}`} data-promo-chaos="availability"><small>DOSTĘPNOŚĆ</small><strong>Piątek — niedostępna</strong><p>Gdzie to zapisać?</p><span className={styles.warning}>! Jeszcze jedno ustalenie</span></div>
    </div>
    <div className={styles.bottomline}><span>Grafik tutaj. Rozmowa tam.</span><span>01 / 09</span></div>
  </section>;
}
