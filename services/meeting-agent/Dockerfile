FROM python:3.11-slim-bookworm

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    WHISPER_MODEL_PATH=/opt/models/whisper-small

RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /worker
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
    && python -c "from faster_whisper.utils import download_model; download_model('small', output_dir='/opt/models/whisper-small')"

COPY app ./app
RUN useradd --system --uid 10001 worker && chown -R worker:worker /worker /opt/models
USER worker

CMD ["python", "-m", "app.main"]
