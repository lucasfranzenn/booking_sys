import { useEffect, useState } from "react";
import "./BookingPage.css";

const API_URL: string = "http://localhost:5215";

type Time = string;

export default function BookingPage() {
  const [date, setDate] = useState<string>("");
  const [times, setTimes] = useState<Time[]>([]);
  const [selectedTime, setSelectedTime] = useState<string>("");
  const [loading, setLoading] = useState<boolean>(false);
  const [message, setMessage] = useState<string>("");

  async function loadTimes(selectedDate: string) {
    if (!selectedDate) return;

    const response = await fetch(
      `${API_URL}/times?date=${selectedDate}`
    );

    if (!response.ok) {
      setMessage("Não foi possível carregar os horários.");
      setTimes([""])
      return;
    }

    const availableTimes: Time[] = await response.json();
    setTimes(availableTimes);
    setSelectedTime("");
  }

  useEffect(() => {
    loadTimes(date);
    setMessage("");
  }, [date]);

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    setLoading(true);
    setMessage("");

    try {
      const response = await fetch(`${API_URL}/booking`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ date, time: selectedTime }),
      });

      if (response.status === 409) {
        setMessage("Esse horário acabou de ser reservado. Atualizei a lista.");
        await loadTimes(date);
        return;
      }

      if (!response.ok) {
        setMessage("Não foi possível fazer a reserva.");
        return;
      }

      setMessage("Reserva confirmada!");
      await loadTimes(date);
    } catch {
      setMessage("Não consegui conectar à API.");
    } finally {
      setLoading(false);
    }
  }

  return (
  <main className="booking">
    <h1>Reservar sala</h1>

    <form className="booking__form" onSubmit={handleSubmit}>
      <label className="booking__label" htmlFor="date">
        Data
      </label>

      <input
        className="booking__date"
        id="date"
        type="date"
        value={date}
        onChange={(event) => setDate(event.target.value)}
        required
      />

      <fieldset className="booking__times">
        <legend>Horário disponível</legend>

        {
          ["09:00:00", "10:00:00", "11:00:00"].map((time) => {
            const available = times.includes(time);

            return (
              <label
                key={time}
                className={available ? "time-slot--available" : "time-slot--unavailable"}>
                <input
                  type="radio"
                  name="time"
                  value={time}
                  checked={selectedTime === time}
                  onChange={() => setSelectedTime(time)}
                  disabled={!available}
                  required
                />
                {time.slice(0, 5)}
                {!available ? " — ocupado": " — disponivel"}
              </label>
            );
          })}
      </fieldset>

      <button
        className="booking__button"
        disabled={loading || !date || !selectedTime}
      >
        {loading ? "Enviando..." : "Reservar"}
      </button>

      <p className="booking__message" role="status">
        {message}
      </p>
    </form>
  </main>
);
}