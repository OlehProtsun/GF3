import { useState } from "react";
import { ContainerGraphMatrix } from "@entities/containers/ui/ContainerGraphMatrix";
import { getGraphCellKey, type GraphMatrixColumn } from "@entities/containers/model/graphWorkspace";
import { setLanguage } from "@shared/i18n";
import { SHOTS, SHOT_DETAILS, fittedPoint, shotAsset, type FeatureId } from "./shot-manifest";
import styles from "./RealFeatureShot.module.css";

export function RealFeatureShot({ feature }: { feature: FeatureId }) {
  const shot = SHOTS.find(shot => shot.id === feature)!;
  const details = SHOT_DETAILS[feature];
  const fit = Math.min(1, 1520 / details.size[0], 620 / details.size[1]);
  const cursor = fittedPoint(details.actionSize, details.actionCursor);
  return <div className={styles.shot} data-promo-real-shot={feature} aria-label={shot.feature}>
    {shot.assets.map((asset, index) => <div key={asset} className={`${styles.state} ${feature === "swap" && index === 2 ? styles.modal : ""}`} data-promo-shot-state={["beginning", "interaction", "outcome"][index]}>
      <img src={shotAsset(asset)} alt={`${shot.feature} — ${["widok", "działanie", "rezultat"][index]}`} draggable={false} />
    </div>)}
    <div className={styles.fragments} aria-hidden="true">
      {details.rects.map(([x, y, width, height], index) => {
        const [left, top] = fittedPoint(details.size, [x, y]);
        return <div key={index} className={styles.fragment} data-promo-fragment={index} data-source-rect={`${x},${y},${width},${height}`} data-source-asset={details.asset}
          data-home-x={left} data-home-y={top} data-home-width={width * fit} data-home-height={height * fit}
          style={{ left: `${left / 1520 * 100}%`, top: `${top / 620 * 100}%`, width: `${width * fit / 1520 * 100}%`, height: `${height * fit / 620 * 100}%` }}>
          <img src={shotAsset(details.asset)} alt="" draggable={false} style={{ width: `${details.size[0] / width * 100}%`, height: `${details.size[1] / height * 100}%`, left: `${-x / width * 100}%`, top: `${-y / height * 100}%` }} />
        </div>;
      })}
    </div>
    <svg data-promo-cursor className={styles.cursor} style={{ left: `${cursor[0] / 1520 * 100}%`, top: `${cursor[1] / 620 * 100}%` }} viewBox="0 0 32 40" aria-hidden="true"><path d="M2 2 L2 30 L10 23 L17 37 L23 34 L16 21 L28 21 Z" fill="#0f172a" stroke="white" strokeWidth="2" /></svg>
    <span data-promo-touch className={styles.touch} style={{ left: `${cursor[0] / 1520 * 100}%`, top: `${cursor[1] / 620 * 100}%` }} aria-hidden="true" />
  </div>;
}

// Isolated acquisition surface, using the production editor and its unmodified CSS.
export function ManagerCapture() {
  const [cells, setCells] = useState<Record<string, string>>(() => {
    setLanguage("pl");
    return Object.fromEntries([1, 2, 3].flatMap(employeeId => [12, 13, 14, 15, 16, 17, 18].map(day =>
      [getGraphCellKey(employeeId, day), day === 18 ? "-" : employeeId === 2 ? "12:00 - 20:00" : "09:00 - 17:00"],
    )));
  });
  const columns: GraphMatrixColumn[] = ["Kasia", "Marta", "Tomek"].map((label, index) => ({
    employeeId: index + 1, kind: "employee", manualColumnId: null, graphEmployeeId: index + 1,
    label, minHoursMonth: null, totalMinutes: 0, totalText: "",
  }));
  return <main className={styles.mount} data-promo-product-mount>
    <h1 className={styles.captureTitle}>GF3 · Sklep Centrum · Październik 2026</h1>
    <ContainerGraphMatrix graph={{ year: 2026, month: 10 }} columns={columns} cellMap={cells} title="Grafik zespołu" helperText="Wybierz komórkę, aby edytować zmianę." showColumnTotals={false} allowColumnResize={false} stretchColumns onCellChange={(employeeId, day, value) => setCells(current => ({ ...current, [getGraphCellKey(employeeId, day)]: value }))} />
  </main>;
}
