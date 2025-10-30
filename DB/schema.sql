PRAGMA foreign_keys = ON;

/* ===========================
   1) Контейнери графіків
   =========================== */
CREATE TABLE container (
  id    INTEGER PRIMARY KEY,
  name  TEXT NOT NULL UNIQUE,    -- напр. "October 2025", "March 2026"
  note  TEXT
);

/* ===========================
   2) Магазини
   =========================== */
CREATE TABLE shop (
  id          INTEGER PRIMARY KEY,
  name        TEXT NOT NULL,
  description TEXT
);

/* ===========================
   3) Працівники
   =========================== */
CREATE TABLE employee (
  id         INTEGER PRIMARY KEY,
  first_name TEXT NOT NULL,
  last_name  TEXT NOT NULL,
  phone      TEXT,               -- може бути NULL
  email      TEXT                -- може бути NULL
);

/* ============================================
   4) Доступність (місячна картка + щоденні рядки)
   ============================================ */
CREATE TABLE availability_month (
  id          INTEGER PRIMARY KEY,
  employee_id INTEGER NOT NULL REFERENCES employee(id) ON DELETE CASCADE,
  year        INTEGER NOT NULL,
  month       INTEGER NOT NULL CHECK (month BETWEEN 1 AND 12),
  UNIQUE(employee_id, year, month)
);

CREATE TABLE availability_day (
  id                      INTEGER PRIMARY KEY,
  availability_month_id   INTEGER NOT NULL REFERENCES availability_month(id) ON DELETE CASCADE,
  day_of_month            INTEGER NOT NULL CHECK (day_of_month BETWEEN 1 AND 31),
  kind                    TEXT NOT NULL CHECK (kind IN ('ANY','NONE','INT')),
  /* якщо kind='INT' — один інтервал рядком: "HH:mm - HH:mm" (дозволимо і без пробілів: "HH:mm-HH:mm") */
  interval_str            TEXT,
  CHECK (
    (kind = 'INT'  AND interval_str IS NOT NULL AND length(trim(interval_str)) >= 11)
    OR
    (kind IN ('ANY','NONE') AND interval_str IS NULL)
  ),
  UNIQUE(availability_month_id, day_of_month)
);

/* ===========================
   5) Графік (місячний) на магазин
   =========================== */
CREATE TABLE schedule (
  id                       INTEGER PRIMARY KEY,
  container_id             INTEGER NOT NULL REFERENCES container(id) ON DELETE RESTRICT,
  shop_id                  INTEGER NOT NULL REFERENCES shop(id) ON DELETE CASCADE,
  name                     TEXT NOT NULL,
  year                     INTEGER NOT NULL,
  month                    INTEGER NOT NULL CHECK (month BETWEEN 1 AND 12),

  people_per_shift         INTEGER NOT NULL CHECK (people_per_shift >= 1),

  /* Часи змін як рядок рівно у форматі "HH:mm - HH:mm" (з провідними нулями) */
  shift1_time              TEXT NOT NULL CHECK (shift1_time LIKE '__:__ - __:__'),
  shift2_time              TEXT NOT NULL CHECK (shift2_time LIKE '__:__ - __:__'),

  max_hours_per_emp_month  INTEGER NOT NULL CHECK (max_hours_per_emp_month >= 0),
  max_consecutive_days     INTEGER NOT NULL CHECK (max_consecutive_days >= 1),
  max_consecutive_full     INTEGER NOT NULL CHECK (max_consecutive_full >= 1),
  max_full_per_month       INTEGER NOT NULL CHECK (max_full_per_month >= 0),

  comment                  TEXT,
  status                   TEXT NOT NULL CHECK (status IN ('DRAFT','PUBLISHED','ARCHIVED'))
);

/* Перевірки зсувів у змінах: 2-га починається рівно там, де закінчується 1-ша;
   і в кожній зміні кінець > початку. */
CREATE TRIGGER schedule_validate_shift_back_to_back_insert
BEFORE INSERT ON schedule
FOR EACH ROW
BEGIN
  /* shift2 start (1..5) == shift1 end (10..14) */
  SELECT CASE
    WHEN substr(NEW.shift1_time, 10, 5) <> substr(NEW.shift2_time, 1, 5)
      THEN raise(ABORT, 'shift2 must start at shift1 end (HH:mm)')
  END;
  /* shift1 end > start */
  SELECT CASE
    WHEN substr(NEW.shift1_time, 1, 5) >= substr(NEW.shift1_time, 10, 5)
      THEN raise(ABORT, 'shift1 end must be after start')
  END;
  /* shift2 end > start */
  SELECT CASE
    WHEN substr(NEW.shift2_time, 1, 5) >= substr(NEW.shift2_time, 10, 5)
      THEN raise(ABORT, 'shift2 end must be after start')
  END;
END;

CREATE TRIGGER schedule_validate_shift_back_to_back_update
BEFORE UPDATE ON schedule
FOR EACH ROW
BEGIN
  SELECT CASE
    WHEN substr(NEW.shift1_time, 10, 5) <> substr(NEW.shift2_time, 1, 5)
      THEN raise(ABORT, 'shift2 must start at shift1 end (HH:mm)')
  END;
  SELECT CASE
    WHEN substr(NEW.shift1_time, 1, 5) >= substr(NEW.shift1_time, 10, 5)
      THEN raise(ABORT, 'shift1 end must be after start')
  END;
  SELECT CASE
    WHEN substr(NEW.shift2_time, 1, 5) >= substr(NEW.shift2_time, 10, 5)
      THEN raise(ABORT, 'shift2 end must be after start')
  END;
END;

/* ===========================
   6) Працівники, включені у графік
   =========================== */
CREATE TABLE schedule_employee (
  id               INTEGER PRIMARY KEY,
  schedule_id      INTEGER NOT NULL REFERENCES schedule(id) ON DELETE CASCADE,
  employee_id      INTEGER NOT NULL REFERENCES employee(id) ON DELETE CASCADE,
  min_hours_month  INTEGER,  -- NULL якщо не задано
  UNIQUE(schedule_id, employee_id)
);

/* ===========================
   7) Слоти змін графіка
   =========================== */
CREATE TABLE schedule_slot (
  id            INTEGER PRIMARY KEY,
  schedule_id   INTEGER NOT NULL REFERENCES schedule(id) ON DELETE CASCADE,
  day_of_month  INTEGER NOT NULL CHECK (day_of_month BETWEEN 1 AND 31),
  shift_no      INTEGER NOT NULL CHECK (shift_no IN (1,2)),
  slot_no       INTEGER NOT NULL CHECK (slot_no >= 1),

  employee_id   INTEGER REFERENCES employee(id) ON DELETE SET NULL,
  status        TEXT NOT NULL DEFAULT 'UNFURNISHED' CHECK (status IN ('UNFURNISHED','ASSIGNED')),

  CHECK ( (status='UNFURNISHED' AND employee_id IS NULL) OR
          (status='ASSIGNED'    AND employee_id IS NOT NULL) ),

  UNIQUE(schedule_id, day_of_month, shift_no, slot_no)
);

/* Заборона ставити одного працівника двічі в одну й ту ж зміну (той самий день+зміна) */
CREATE UNIQUE INDEX ux_slot_unique_emp_per_shift
  ON schedule_slot(schedule_id, day_of_month, shift_no, employee_id)
  WHERE employee_id IS NOT NULL;

/* ===========================
   8) Індекси для продуктивності
   =========================== */
CREATE INDEX ix_avail_month_emp        ON availability_month(employee_id, year, month);
CREATE INDEX ix_avail_day_month        ON availability_day(availability_month_id, day_of_month);
CREATE INDEX ix_sched_container        ON schedule(container_id);
CREATE INDEX ix_sched_shop_month       ON schedule(shop_id, year, month);
CREATE INDEX ix_sched_container_shop   ON schedule(container_id, shop_id);
CREATE INDEX ix_slot_sched_day_shift   ON schedule_slot(schedule_id, day_of_month, shift_no);
