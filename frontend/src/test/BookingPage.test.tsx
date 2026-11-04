import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import BookingPage from "../Pages/BookingPage";

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

describe("BookingPage", () => {
  it("avisa que o horário foi ocupado e atualiza a lista após receber 409", async () => {
    const user = userEvent.setup();

    const fetchMock = vi
      .fn()
      // Primeira consulta: todos os horários disponíveis
      .mockResolvedValueOnce(
        new Response(JSON.stringify(["09:00:00", "10:00:00", "11:00:00"]), {
          status: 200,
        }),
      )
      // Tentativa de reserva: conflito
      .mockResolvedValueOnce(new Response("Horário ocupado", { status: 409 }))
      // Consulta após o conflito: 10 h já não aparece
      .mockResolvedValueOnce(
        new Response(JSON.stringify(["09:00:00", "11:00:00"]), {
          status: 200,
        }),
      );

    vi.stubGlobal("fetch", fetchMock);

    render(<BookingPage />);

    await user.type(screen.getByLabelText("Data"), "2099-02-04");

    const tenOclock = await screen.findByLabelText(/10:00/);
    await user.click(tenOclock);
    await user.click(screen.getByRole("button", { name: "Reservar" }));

    expect(
      await screen.findByText(/Esse horário acabou de ser reservado/),
    ).toBeInTheDocument();

    await waitFor(() => {
      expect(screen.getByLabelText(/10:00/)).toBeDisabled();
    });

    expect(fetchMock).toHaveBeenCalledTimes(3);
    expect(fetchMock.mock.calls[1][0]).toContain("/booking");
  });
});