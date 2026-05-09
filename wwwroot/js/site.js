(() => {
  const appointmentInputs = document.querySelectorAll("[data-min-now]");
  if (appointmentInputs.length === 0) {
    return;
  }

  const pad = (value) => value.toString().padStart(2, "0");
  const toLocalInputValue = (date) => {
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  };

  const minimumDate = new Date(Date.now() + 5 * 60 * 1000);
  appointmentInputs.forEach((input) => {
    input.min = toLocalInputValue(minimumDate);
  });
})();
